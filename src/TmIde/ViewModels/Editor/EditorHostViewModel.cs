using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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

    private readonly ICodeAnalysisService _analysisService;

    public EditorHostViewModel(ICodeAnalysisService? analysis = null)
    {
        _analysisService = analysis ?? new CodeAnalysisService();
        Documents = new ObservableCollection<EditorDocumentViewModel>();
        Breadcrumb = new ObservableCollection<BreadcrumbSegmentViewModel>();
    }

    public ObservableCollection<EditorDocumentViewModel> Documents { get; }

    public ObservableCollection<BreadcrumbSegmentViewModel> Breadcrumb { get; }

    /// <summary>Carpeta del proyecto abierto; el breadcrumb parte de ella.</summary>
    public string? ProjectDirectory { get; set; }

    public bool HasUnsavedChanges => Documents.Any(document => document.IsModified);

    /// <summary>
    /// Abre (o activa, si ya está abierto) el archivo de <paramref name="filePath"/>
    /// con su contenido en disco. Lanza <see cref="IOException"/> si no se puede leer.
    /// </summary>
    public void OpenDocument(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);

        var existing = Documents.FirstOrDefault(d => string.Equals(d.FilePath, fullPath, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            SelectedDocument = existing;
            return;
        }

        var document = new EditorDocumentViewModel(
            Path.GetFileName(fullPath), RelativePathOf(fullPath), File.ReadAllText(fullPath),
            CloseDocument, _analysisService, filePath: fullPath, openDocumentTexts: OpenDocumentTexts);

        Documents.Add(document);
        SelectedDocument = document;
    }

    /// <summary>Cierra todos los documentos sin guardarlos.</summary>
    public void CloseAll()
    {
        foreach (var document in Documents.ToList())
        {
            document.CloseCommand.Execute(null);
        }
    }

    // Ruta desde la carpeta del proyecto, incluida, separada por '/': "importaciones/principal.tmpr".
    private string RelativePathOf(string fullPath)
    {
        if (ProjectDirectory is null)
        {
            return Path.GetFileName(fullPath);
        }

        var projectName = Path.GetFileName(Path.TrimEndingDirectorySeparator(ProjectDirectory));
        var relative = Path.GetRelativePath(ProjectDirectory, fullPath);

        return $"{projectName}/{relative.Replace(Path.DirectorySeparatorChar, '/')}";
    }

    /// <summary>
    /// Texto de cada documento abierto, por ruta completa. El compilador lo usa
    /// en lugar del contenido en disco. Solo desde el hilo de la UI.
    /// </summary>
    private IReadOnlyDictionary<string, string> OpenDocumentTexts()
    {
        var texts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var document in Documents)
        {
            texts[document.FilePath] = document.Content;
        }

        return texts;
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
