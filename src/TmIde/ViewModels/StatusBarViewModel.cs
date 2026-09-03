using System.ComponentModel;

using CommunityToolkit.Mvvm.ComponentModel;

using TmIde.ViewModels.Editor;

namespace TmIde.ViewModels;

/// <summary>
/// Barra de estado: los botones COMPILE / RUN / DEBUG a la izquierda son solo
/// visuales (sin acción); a la derecha muestra <c>LN x, COL y   UTF-8</c>.
/// </summary>

public sealed partial class StatusBarViewModel : ViewModelBase
{
    private readonly EditorHostViewModel _editor;

    [ObservableProperty]
    private string _caretPosition = "LN 1, COL 1";

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
    }

    private void OnDocumentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EditorDocumentViewModel.CaretLine)
            or nameof(EditorDocumentViewModel.CaretColumn))
        {
            UpdateCaret();
        }
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
