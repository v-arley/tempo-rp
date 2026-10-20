using System;
using System.ComponentModel;
using System.IO;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using TmIde.Services;
using TmIde.ViewModels.Editor;
using TmIde.ViewModels.Explorer;
using TmIde.ViewModels.Panel;
using TmIde.ViewModels.Shell;

namespace TmIde.ViewModels;

/// <summary>
/// ViewModel raíz de la ventana principal. Agrega los ViewModels de cada región
/// del shell y los conecta entre sí (la ventana de inicio abre un proyecto; el
/// explorador abre documentos en el editor; menús y barra de estado escriben en
/// el panel inferior).
/// </summary>

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IThemeService _themeService;
    private readonly IStartDialogService _startDialog;
    private readonly Func<string, bool> _confirm;

    [ObservableProperty]
    private string _title = "TMIDE";

    /// <param name="bottomPanel">
    /// Se recibe ya creado porque otros servicios (el diálogo de inicio) también
    /// escriben en él.
    /// </param>
    /// <param name="confirm">Pregunta sí/no al usuario; <c>true</c> si acepta.</param>
    public MainWindowViewModel(
        IThemeService themeService,
        IStartDialogService startDialog,
        BottomPanelViewModel bottomPanel,
        Func<string, bool> confirm)
    {
        _themeService = themeService;
        _startDialog = startDialog;
        _confirm = confirm;

        EditorHost = new EditorHostViewModel();
        BottomPanel = bottomPanel;
        Explorer = new FileExplorerViewModel();
        MenuBar = new MenuBarViewModel(themeService, ShowStart, Save, BottomPanel.Log);
        StatusBar = new StatusBarViewModel(EditorHost);

        EditorHost.PropertyChanged += OnEditorHostPropertyChanged;
        UpdateTitle();
    }

    public MenuBarViewModel MenuBar { get; }

    public FileExplorerViewModel Explorer { get; }

    public EditorHostViewModel EditorHost { get; }

    public BottomPanelViewModel BottomPanel { get; }

    public StatusBarViewModel StatusBar { get; }

    /// <summary>Carpeta del proyecto abierto, o <c>null</c> si no hay ninguno.</summary>
    public string? ProjectDirectory { get; private set; }

    [RelayCommand]
    private void ToggleTheme()
    {
        _themeService.Toggle();
        BottomPanel.Log($"Tema activo: {_themeService.Current}");
    }

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
        if (EditorHost.HasUnsavedChanges &&
            !_confirm("Hay cambios sin guardar. ¿Descartarlos y abrir el otro proyecto?"))
        {
            return;
        }

        var fullPath = Path.GetFullPath(projectDirectory);

        try
        {
            Explorer.Load(fullPath, OpenFile);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            BottomPanel.Log($"No se pudo abrir el proyecto {fullPath}: {exception.Message}");
            return;
        }

        EditorHost.CloseAll();
        EditorHost.ProjectDirectory = fullPath;
        ProjectDirectory = fullPath;
        BottomPanel.Log($"Proyecto abierto: {fullPath}");
    }

    private void OpenFile(string filePath)
    {
        try
        {
            EditorHost.OpenDocument(filePath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            BottomPanel.Log($"No se pudo abrir {filePath}: {exception.Message}");
        }
    }

    /// <summary>Guarda el documento activo (Ctrl+S).</summary>
    [RelayCommand]
    private void Save()
    {
        if (EditorHost.SelectedDocument is not { } document)
        {
            return;
        }

        try
        {
            document.Save();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            BottomPanel.Log($"No se pudo guardar {document.FilePath}: {exception.Message}");
        }
    }

    private void OnEditorHostPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorHostViewModel.SelectedDocument))
        {
            UpdateTitle();
        }
    }

    private void UpdateTitle()
    {
        Title = EditorHost.SelectedDocument is { } document
            ? document.FileName.ToUpperInvariant()
            : "TMIDE";
    }
}
