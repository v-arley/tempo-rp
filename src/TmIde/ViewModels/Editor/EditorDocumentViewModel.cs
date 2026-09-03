using System;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using TmIde.ViewModels;

namespace TmIde.ViewModels.Editor;

/// <summary>
/// Un documento abierto en el editor (una pestaña). <see cref="Content"/> es
/// texto plano editable y es la entrada que consume el resaltado de sintaxis.
/// </summary>

public sealed partial class EditorDocumentViewModel : ViewModelBase
{
    private readonly Action<EditorDocumentViewModel>? _onClose;

    [ObservableProperty]
    private string _content;

    [ObservableProperty]
    private bool _isModified;

    [ObservableProperty]
    private int _caretLine = 1;

    [ObservableProperty]
    private int _caretColumn = 1;

    public EditorDocumentViewModel(string fileName, string relativePath, string content, Action<EditorDocumentViewModel>? onClose = null)
    {
        FileName = fileName;
        RelativePath = relativePath;
        _content = content;
        _onClose = onClose;
    }

    public string FileName { get; }

    /// <summary>Ruta relativa del archivo, separada por <c>/</c>, que alimenta el breadcrumb.</summary>
    public string RelativePath { get; }

    [RelayCommand]
    private void Close() => _onClose?.Invoke(this);
}
