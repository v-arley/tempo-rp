using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Microsoft.Extensions.Logging;

using Tempo.Language;
using Tempo.Language.Evaluation;

using TmIde.Services;
using TmIde.Services.Execution;
using TmIde.Services.Settings;
using TmIde.ViewModels.Editor;
using TmIde.ViewModels.Explorer;
using TmIde.ViewModels.Panel;

namespace TmIde.ViewModels;

/// <summary>
/// Proyecto abierto y sus operaciones: abrir, guardar, compilar, ejecutar y
/// detener. Es dueño de los <see cref="Commands"/> que comparten el menú, la
/// barra de estado y los atajos de la ventana principal; vive aparte de
/// <see cref="MainWindowViewModel"/> para que esas regiones puedan resolverse
/// del contenedor sin depender de la ventana.
/// </summary>

public sealed partial class ProjectWorkspaceViewModel : ViewModelBase
{
    private readonly IStartDialogService _startDialog;
    private readonly IConfirmDialogService _confirmDialog;
    private readonly EditorHostViewModel _editorHost;
    private readonly FileExplorerViewModel _explorer;
    private readonly BottomPanelViewModel _bottomPanel;
    private readonly IRecentProjectsService _recentProjects;
    private readonly ILogger<ProjectWorkspaceViewModel> _logger;

    // Dispatcher del hilo de la UI, donde se crea este ViewModel: la ejecución
    // escribe en el panel Run a través de él desde el hilo del intérprete.
    private readonly Dispatcher _uiDispatcher = Dispatcher.CurrentDispatcher;

    /// <summary>Hay un programa en ejecución; STOP solo está activo entonces.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    private bool _isRunning;

    private CancellationTokenSource? _runCancellation;

    public ProjectWorkspaceViewModel(
        IStartDialogService startDialog,
        IConfirmDialogService confirmDialog,
        EditorHostViewModel editorHost,
        FileExplorerViewModel explorer,
        BottomPanelViewModel bottomPanel,
        IRecentProjectsService recentProjects,
        IMessenger messenger,
        ILogger<ProjectWorkspaceViewModel> logger)
    {
        _startDialog = startDialog;
        _confirmDialog = confirmDialog;
        _editorHost = editorHost;
        _explorer = explorer;
        _bottomPanel = bottomPanel;
        _recentProjects = recentProjects;
        _logger = logger;
        _bottomPanel.Output.DiagnosticActivated += OnDiagnosticActivated;
        _explorer.ConfigureFileOperations(_editorHost, _confirmDialog);

        Commands = new ProjectCommands(ShowStartCommand, SaveCommand, CompileCommand, RunCommand, StopCommand);

        messenger.Register<ProjectWorkspaceViewModel, OpenFileRequestedMessage>(
            this, static (workspace, message) => workspace.OpenFile(message.FilePath));
    }

    public ProjectCommands Commands { get; }

    /// <summary>Carpeta del proyecto abierto, o <c>null</c> si no hay ninguno.</summary>
    public string? ProjectDirectory { get; private set; }

    /// <summary>Abre la ventana de inicio y carga el proyecto que se elija en ella.</summary>
    [RelayCommand]
    private void ShowStart()
    {
        if (_startDialog.Show() is { } projectDirectory)
        {
            OpenProject(projectDirectory);
        }
    }

    /// <summary>
    /// Carga la carpeta en el explorador y cierra los documentos del proyecto
    /// anterior, preguntando antes si alguno tiene cambios sin guardar.
    /// </summary>
    public void OpenProject(string projectDirectory)
    {
        if (_editorHost.HasUnsavedChanges &&
            !_confirmDialog.Confirm("Hay cambios sin guardar. ¿Descartarlos y abrir el otro proyecto?"))
        {
            return;
        }

        var fullPath = Path.GetFullPath(projectDirectory);

        // El programa en ejecución pertenece al proyecto anterior.
        _runCancellation?.Cancel();

        try
        {
            _explorer.Load(fullPath);
        }
        catch (Exception exception) when (IsFileSystemError(exception))
        {
            _bottomPanel.Log($"No se pudo abrir el proyecto {fullPath}: {exception.Message}");
            return;
        }

        _editorHost.SwitchToProject(fullPath);
        _bottomPanel.Output.ClearDiagnosticLinks();
        ProjectDirectory = fullPath;
        _recentProjects.Add(fullPath);
        _bottomPanel.Log($"Proyecto abierto: {fullPath}");
    }

    /// Navega al archivo, línea y columna de un diagnóstico seleccionado desde el panel Output
    private void OnDiagnosticActivated(
        DiagnosticNavigationTarget target)
    {
        if (ProjectDirectory is null)
        {
            return;
        }

        if (!File.Exists(target.FilePath))
        {
            _bottomPanel.Log(
                $"OUTPUT → El archivo ya no existe: {target.FilePath}");

            return;
        }

        try
        {
            _editorHost.NavigateToDiagnostic(
                target.FilePath,
                target.Line,
                target.Column);
        }
        catch (Exception exception) when (
            IsFileSystemError(exception))
        {
            _bottomPanel.Log(
                $"OUTPUT → No se pudo abrir el archivo: " +
                exception.Message);
        }
    }

    private void OpenFile(string filePath)
    {
        try
        {
            _editorHost.OpenDocument(filePath);
        }
        catch (Exception exception) when (IsFileSystemError(exception))
        {
            _bottomPanel.Log($"No se pudo abrir {filePath}: {exception.Message}");
        }
    }

    /// <summary>Guarda el documento activo (Ctrl+S).</summary>
    [RelayCommand]
    private void Save()
    {
        if (_editorHost.SelectedDocument is not { } document)
        {
            return;
        }

        try
        {
            document.Save();
        }
        catch (Exception exception) when (IsFileSystemError(exception))
        {
            _bottomPanel.Log($"No se pudo guardar {document.FilePath}: {exception.Message}");
        }
    }

    /// <summary>
    /// Compila el proyecto abierto, con el texto de los documentos abiertos en
    /// lugar del disco, y escribe en Output los diagnósticos de todos sus
    /// archivos con el formato de la §7.1, igual que la CLI.
    /// </summary>
    [RelayCommand]
    private Task CompileAsync() => CompileProjectAsync();

    /// <returns>La compilación, o <c>null</c> si no hay proyecto o no se pudo compilar.</returns>
    private async Task<Compilation?> CompileProjectAsync()
    {
        if (ProjectDirectory is not { } projectDirectory)
        {
            _bottomPanel.Log("COMPILE → Abre un proyecto antes de compilar.");
            return null;
        }

        // Instantánea en el hilo de la UI; la compilación corre fuera de él.
        var openDocuments = _editorHost.OpenDocumentTexts();
        _bottomPanel.Log($"COMPILE → {projectDirectory}");

        Compilation compilation;
        try
        {
            compilation = await Task.Run(() => Compilation.FromPath(projectDirectory, openDocuments));
        }
        catch (Exception exception) when (IsFileSystemError(exception))
        {
            _bottomPanel.Log($"COMPILE → No se pudo leer el proyecto: {exception.Message}");
            return null;
        }
        catch (Exception exception)
        {
            // Un fallo interno del compilador no debe tumbar el IDE.
            LogCompilerFailure(exception, projectDirectory);
            _bottomPanel.Log($"COMPILE → Error interno del compilador: {exception.Message}");
            return null;
        }

        
        foreach (var diagnostic in compilation.Diagnostics)
        {
            var sourceFile = compilation.Project.Files
                .FirstOrDefault(file =>
                    string.Equals(
                        file.FileName,
                        diagnostic.FileName,
                        StringComparison.Ordinal));

            var filePath = sourceFile?.FullPath;

            _bottomPanel.LogDiagnostic(
                diagnostic,
                filePath);
        }


        var errors = compilation.Diagnostics.Count(diagnostic => diagnostic.IsError);
        var warnings = compilation.Diagnostics.Count - errors;
        _bottomPanel.Log($"COMPILE → {errors} error(es), {warnings} advertencia(s).");

        return compilation;
    }

    /// <summary>
    /// Compila el proyecto abierto y, si no tiene errores y su entrada es un
    /// <c>.tmpr</c> con <c>session</c>, lo ejecuta en el panel Run.
    /// </summary>
    [RelayCommand]
    private async Task RunAsync()
    {
        if (await CompileProjectAsync() is not { } compilation)
        {
            return;
        }

        if (compilation.HasErrors)
        {
            _bottomPanel.Log("RUN → Corrige los errores antes de ejecutar.");
        }
        else if (!compilation.IsExecutable)
        {
            _bottomPanel.Log("RUN → El proyecto no tiene un .tmpr con session que ejecutar.");
        }
        else
        {
            await ExecuteInRunPaneAsync(compilation);
        }
    }

    /// <summary>Detiene el programa en ejecución antes de su siguiente sentencia.</summary>
    [RelayCommand(CanExecute = nameof(IsRunning))]
    private void Stop() => _runCancellation?.Cancel();

    // El intérprete corre en otro hilo; su salida y su entrada pasan por el panel
    // Run, y los errores R### se muestran ahí con el formato de la §7.1.
    private async Task ExecuteInRunPaneAsync(Compilation compilation)
    {
        var pane = _bottomPanel.Run;
        pane.Clear();
        _bottomPanel.SelectedPane = pane;

        using var cancellation = new CancellationTokenSource();
        using var input = new IdeRuntimeInput(pane, _uiDispatcher, cancellation.Token);
        var output = new IdeRuntimeOutput(pane, _uiDispatcher);

        _runCancellation = cancellation;
        IsRunning = true;

        try
        {
            await Task.Run(() => compilation.Run(input, output, cancellation.Token));
            pane.WriteStatus("── Ejecución terminada");
        }
        catch (TmRuntimeException exception)
        {
            pane.WriteStatus(exception.ToDiagnostic(compilation.Project.EntryFile.FileName).ToStandardFormat());
        }
        catch (OperationCanceledException)
        {
            pane.WriteStatus("── Ejecución detenida");
        }
        catch (Exception exception)
        {
            // Un fallo interno del intérprete no debe tumbar el IDE.
            LogInterpreterFailure(exception, compilation.Project.EntryFile.FileName);
            pane.WriteStatus($"── Error interno del intérprete: {exception.Message}");
        }
        finally
        {
            pane.StopWaitingForInput();
            _runCancellation = null;
            IsRunning = false;
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Fallo interno del compilador al compilar {ProjectDirectory}")]
    private partial void LogCompilerFailure(Exception exception, string projectDirectory);

    [LoggerMessage(Level = LogLevel.Error, Message = "Fallo interno del intérprete al ejecutar {EntryFile}")]
    private partial void LogInterpreterFailure(Exception exception, string entryFile);

    private static bool IsFileSystemError(Exception exception) =>
        exception is IOException or UnauthorizedAccessException;
}
