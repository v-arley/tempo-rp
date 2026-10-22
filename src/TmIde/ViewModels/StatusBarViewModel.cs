using System.ComponentModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using TmIde.ViewModels.Editor;

namespace TmIde.ViewModels;

/// <summary>
/// Barra de estado: a la izquierda muestra <c>LN x, COL y   UTF-8</c> y el
/// recuento de problemas; a la derecha, COMPILE, RUN y STOP sobre el proyecto
/// abierto.
/// </summary>

public sealed partial class StatusBarViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _caretPosition = "LN 1, COL 1";

    /// <summary>Recuento de errores y advertencias del documento activo; vacío si no hay ninguno.</summary>
    [ObservableProperty]
    private string _problemsSummary = string.Empty;

    private EditorDocumentViewModel? _trackedDocument;

    /// <param name="messenger">Por él llega la pestaña activa (<see cref="ActiveDocumentChangedMessage"/>).</param>
    public StatusBarViewModel(ProjectCommands commands, IMessenger messenger)
    {
        Commands = commands;

        messenger.Register<StatusBarViewModel, ActiveDocumentChangedMessage>(
            this, static (statusBar, message) => statusBar.TrackDocument(message.Document));
        TrackDocument(null);
    }

    public string Encoding => "UTF-8";

    /// <summary>Comandos de los botones COMPILE / RUN / STOP.</summary>
    public ProjectCommands Commands { get; }

    private void TrackDocument(EditorDocumentViewModel? document)
    {
        if (_trackedDocument is not null)
        {
            _trackedDocument.PropertyChanged -= OnDocumentPropertyChanged;
        }

        _trackedDocument = document;

        if (_trackedDocument is not null)
        {
            _trackedDocument.PropertyChanged += OnDocumentPropertyChanged;
        }

        UpdateCaret();
        UpdateProblems();
    }

    private void OnDocumentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EditorDocumentViewModel.CaretLine)
            or nameof(EditorDocumentViewModel.CaretColumn))
        {
            UpdateCaret();
        }
        else if (e.PropertyName is nameof(EditorDocumentViewModel.ErrorCount)
            or nameof(EditorDocumentViewModel.WarningCount))
        {
            UpdateProblems();
        }
    }

    // Vacío cuando no hay problemas: la barra no muestra ruido en un archivo limpio.
    private void UpdateProblems()
    {
        ProblemsSummary = _trackedDocument is { } document && (document.ErrorCount > 0 || document.WarningCount > 0)
            ? $"✕ {document.ErrorCount}   ⚠ {document.WarningCount}"
            : string.Empty;
    }

    private void UpdateCaret()
    {
        if (_trackedDocument is null)
        {
            CaretPosition = "LN -, COL -";
            return;
        }

        CaretPosition = $"LN {_trackedDocument.CaretLine}, COL {_trackedDocument.CaretColumn}";
    }
}
