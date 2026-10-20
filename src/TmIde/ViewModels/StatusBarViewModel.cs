using System.ComponentModel;

using CommunityToolkit.Mvvm.ComponentModel;

using TmIde.ViewModels.Editor;

namespace TmIde.ViewModels;

/// <summary>
/// Barra de estado: a la izquierda muestra <c>LN x, COL y   UTF-8</c>; los botones
/// COMPILE / RUN / DEBUG a la derecha son solo visuales (sin acción).
/// </summary>

public sealed partial class StatusBarViewModel : ViewModelBase
{
    private readonly EditorHostViewModel _editor;

    [ObservableProperty]
    private string _caretPosition = "LN 1, COL 1";

    /// <summary>Recuento de errores y advertencias del documento activo; vacío si no hay ninguno.</summary>
    [ObservableProperty]
    private string _problemsSummary = string.Empty;

    private EditorDocumentViewModel? _trackedDocument;

    public StatusBarViewModel(EditorHostViewModel editor)
    {
        _editor = editor;

        _editor.PropertyChanged += OnEditorPropertyChanged;
        TrackDocument(_editor.SelectedDocument);
    }

    public string Encoding => "UTF-8";

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorHostViewModel.SelectedDocument))
        {
            TrackDocument(_editor.SelectedDocument);
        }
    }

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
