using System;
using System.Collections.ObjectModel;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using TmIde.ViewModels;

namespace TmIde.ViewModels.Editor;

/// <summary>
/// Área central: pestañas de documentos + breadcrumb. Arranca sin documentos
/// abiertos; se llena cuando el explorador abre un archivo.
/// </summary>

public sealed partial class EditorHostViewModel : ViewModelBase
{
    [ObservableProperty]
    private EditorDocumentViewModel? _selectedDocument;

    public EditorHostViewModel()
    {
        Documents = new ObservableCollection<EditorDocumentViewModel>();
        Breadcrumb = new ObservableCollection<BreadcrumbSegmentViewModel>();
    }

    public ObservableCollection<EditorDocumentViewModel> Documents { get; }

    public ObservableCollection<BreadcrumbSegmentViewModel> Breadcrumb { get; }

    /// <summary>Abre (o activa, si ya existe) un documento por nombre de archivo.</summary>
    public void OpenDocument(string fileName)
    {
        var existing = Documents.FirstOrDefault(d => d.FileName == fileName);
        if (existing is not null)
        {
            SelectedDocument = existing;
            return;
        }

        var document = new EditorDocumentViewModel(fileName, fileName, string.Empty, CloseDocument);

        Documents.Add(document);
        SelectedDocument = document;
    }

    private void CloseDocument(EditorDocumentViewModel document)
    {
        var index = Documents.IndexOf(document);
        if (index < 0)
        {
            return;
        }

        Documents.Remove(document);

        if (SelectedDocument == document)
        {
            SelectedDocument = Documents.Count == 0
                ? null
                : Documents[Math.Min(index, Documents.Count - 1)];
        }
    }

    partial void OnSelectedDocumentChanged(EditorDocumentViewModel? value)
    {
        Breadcrumb.Clear();
        if (value is null)
        {
            return;
        }

        var parts = value.RelativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            Breadcrumb.Add(new BreadcrumbSegmentViewModel(parts[i].ToUpperInvariant(), isLast: i == parts.Length - 1));
        }
    }
}
