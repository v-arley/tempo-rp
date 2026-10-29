
using System.ComponentModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using TmIde.ViewModels.Editor;

namespace TmIde.ViewModels;

/// <summary>
/// Controla la información de la barra inferior:
/// cursor, codificación, problemas y estado del proyecto.
/// </summary>
public sealed partial class StatusBarViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _caretPosition = "LN 1, COL 1";

    [ObservableProperty]
    private string _problemsSummary = string.Empty;

    [ObservableProperty]
    private ProjectActivityState _projectState =
        ProjectActivityState.NoProject;

    private EditorDocumentViewModel? _trackedDocument;

    public StatusBarViewModel(
        ProjectCommands commands,
        IMessenger messenger)
    {
        Commands = commands;

        // Actualizar información del documento activo
        messenger.Register<
            StatusBarViewModel,
            ActiveDocumentChangedMessage>(
            this,
            static (statusBar, message) =>
                statusBar.TrackDocument(message.Document));

        // Escuchar cambios del estado del proyecto
        messenger.Register<
            StatusBarViewModel,
            ProjectActivityChangedMessage>(
            this,
            static (statusBar, message) =>
                statusBar.ProjectState = message.State);

        TrackDocument(null);
    }

    public string Encoding => "UTF-8";

    public ProjectCommands Commands { get; }

    /// Texto visible en el centro de la barra inferior.
    public string ProjectStatusText => ProjectState switch
    {
        ProjectActivityState.NoProject =>
            "○ SIN PROYECTO",

        ProjectActivityState.Ready =>
            "● LISTO",

        ProjectActivityState.Compiling =>
            "◉ COMPILANDO...",

        ProjectActivityState.Compiled =>
            "● COMPILADO",

        ProjectActivityState.WithErrors =>
            "● CON ERRORES",

        ProjectActivityState.Running =>
            "▶ EJECUTANDO...",

        _ => "○ SIN PROYECTO"
    };

    /// Notificar que el texto del estado cambió
    partial void OnProjectStateChanged(
        ProjectActivityState value)
    {
        OnPropertyChanged(nameof(ProjectStatusText));
    }

    private void TrackDocument(
        EditorDocumentViewModel? document)
    {
        if (_trackedDocument is not null)
        {
            _trackedDocument.PropertyChanged -=
                OnDocumentPropertyChanged;
        }

        _trackedDocument = document;

        if (_trackedDocument is not null)
        {
            _trackedDocument.PropertyChanged +=
                OnDocumentPropertyChanged;
        }

        UpdateCaret();
        UpdateProblems();
    }

    private void OnDocumentPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName is
            nameof(EditorDocumentViewModel.CaretLine)
            or nameof(EditorDocumentViewModel.CaretColumn))
        {
            UpdateCaret();
        }
        else if (e.PropertyName is
            nameof(EditorDocumentViewModel.ErrorCount)
            or nameof(EditorDocumentViewModel.WarningCount))
        {
            UpdateProblems();
        }
    }

    /// Actualiza el recuento de errores y advertenciasdel documento activo
    private void UpdateProblems()
    {
        ProblemsSummary =
            _trackedDocument is { } document &&
            (document.ErrorCount > 0 ||
             document.WarningCount > 0)
                ? $"✕ {document.ErrorCount}   " +
                  $"⚠ {document.WarningCount}"
                : string.Empty;
    }

    /// Actualiza la posición actual del cursor
    private void UpdateCaret()
    {
        if (_trackedDocument is null)
        {
            CaretPosition = "LN -, COL -";
            return;
        }

        CaretPosition =
            $"LN {_trackedDocument.CaretLine}, " +
            $"COL {_trackedDocument.CaretColumn}";
    }
}
