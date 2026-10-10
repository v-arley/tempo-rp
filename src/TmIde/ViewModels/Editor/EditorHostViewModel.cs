using System;
using System.Collections.ObjectModel;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using TmIde.Services.Analysis;
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

    private readonly ICodeAnalysisService _analysis;

    public EditorHostViewModel(ICodeAnalysisService? analysis = null)
    {
        _analysis = analysis ?? new CodeAnalysisService();
        Documents = new ObservableCollection<EditorDocumentViewModel>();
        Breadcrumb = new ObservableCollection<BreadcrumbSegmentViewModel>();

        AddPlaceholderDocuments();
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

        var document = new EditorDocumentViewModel(fileName, fileName, string.Empty, CloseDocument, _analysis);

        Documents.Add(document);
        SelectedDocument = document;
    }

    // TEMPORAL: documentos de relleno para visualizar las pestañas del editor
    // mientras no hay apertura real de archivos. Eliminar al conectar el explorador.
    private void AddPlaceholderDocuments()
    {
        Documents.Add(new EditorDocumentViewModel(
            "main.tm", "src/main.tm",
            "# main.tm — documento de relleno\n# Punto de entrada del programa TM.\n",
            CloseDocument, _analysis));
        Documents.Add(new EditorDocumentViewModel(
            "matematicas.tm", "src/lib/matematicas.tm",
            "# matematicas.tm — documento de relleno\n# Funciones auxiliares importadas desde main.tm.\n",
            CloseDocument, _analysis));

        SelectedDocument = Documents[0];
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
