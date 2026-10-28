using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

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

    /// Solicitud de navegacion hacia una posicion del editor.
    [ObservableProperty]
    private EditorNavigationRequest? _navigationRequest;

    private readonly ICodeAnalysisService _analysisService;
    private readonly IMessenger _messenger;

    /// <param name="messenger">Por él se avisa el cambio de pestaña activa (<see cref="ActiveDocumentChangedMessage"/>).</param>
    public EditorHostViewModel(ICodeAnalysisService analysis, IMessenger messenger)
    {
        _analysisService = analysis;
        _messenger = messenger;
        Documents = new ObservableCollection<EditorDocumentViewModel>();
        Breadcrumb = new ObservableCollection<BreadcrumbSegmentViewModel>();
    }

    public ObservableCollection<EditorDocumentViewModel> Documents { get; }

    public ObservableCollection<BreadcrumbSegmentViewModel> Breadcrumb { get; }

    /// <summary>Carpeta del proyecto abierto; el breadcrumb parte de ella.</summary>
    public string? ProjectDirectory { get; private set; }

    public bool HasUnsavedChanges => Documents.Any(document => document.IsModified);

    /// <summary>Cierra los documentos del proyecto anterior, sin guardarlos, y pasa al nuevo.</summary>
    public void SwitchToProject(string projectDirectory)
    {
        foreach (var document in Documents.ToList())
        {
            document.Close();
        }

        ProjectDirectory = projectDirectory;
    }

    
    /// Abre un archivo, selecciona su pestaña y solicita mover el cursor a la línea y columna del diagnóstico
    public void NavigateToDiagnostic(
        string filePath,
        int line,
        int? column)
    {
        // Abre el archivo si todavía no está abierto
        // Si ya existe su pestaña, solamente la activa
        OpenDocument(filePath);

        if (SelectedDocument is not { } selected)
        {
            return;
        }

        var document = selected.Document;

        // El compilador puede informar una línea fuera del rango del archivo si este fue modificado
        var safeLine = Math.Clamp(
            line,
            1,
            document.LineCount);

        var documentLine = document.GetLineByNumber(safeLine);

        var safeColumn = Math.Clamp(
            column ?? 1,
            1,
            documentLine.Length + 1);

        var offset = documentLine.Offset + safeColumn - 1;

        // Cada solicitud es una instancia nueva
        NavigationRequest = new EditorNavigationRequest(
            document,
            offset);
    }


    /// Inserta un ejemplo en la posicion actual del cursor del documento activo.
    /// No reemplaza todo el archivo.
    /// No guarda automaticamente.
    public bool InsertExampleAtCaret(string example)
    {
        if (SelectedDocument is not { } selected ||
            string.IsNullOrEmpty(example))
        {
            return false;
        }

        var document = selected.Document;

        var lineNumber = Math.Clamp(
            selected.CaretLine,
            1,
            document.LineCount);

        var line = document.GetLineByNumber(lineNumber);

        var column = Math.Clamp(
            selected.CaretColumn,
            1,
            line.Length + 1);

        var offset = line.Offset + column - 1;

        document.Insert(offset, example);

        NavigationRequest = new EditorNavigationRequest(
            document,
            offset + example.Length);

        return true;
}

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
            fullPath, RelativePathOf(fullPath), File.ReadAllText(fullPath),
            CloseDocument, _analysisService, OpenDocumentTexts);
        document.ContentChanged += OnDocumentContentChanged;

        Documents.Add(document);
        SelectedDocument = document;
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
    /// en lugar del contenido en disco: lo que se compila es lo que el usuario ve,
    /// aunque no lo haya guardado. Solo desde el hilo de la UI.
    /// </summary>
    public IReadOnlyDictionary<string, string> OpenDocumentTexts()
    {
        var texts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var document in Documents)
        {
            texts[document.FilePath] = document.Content;
        }

        return texts;
    }

    public IReadOnlyList<EditorDocumentViewModel> DocumentsAtOrUnder(string path)
    {
        var fullPath = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(path));

        var prefix = fullPath + Path.DirectorySeparatorChar;

        return Documents
            .Where(document =>
                string.Equals(
                    document.FilePath,
                    fullPath,
                    StringComparison.OrdinalIgnoreCase) ||
                document.FilePath.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public void ApplyPathRename(
        string originalPath,
        string renamedPath)
    {
        var oldPath = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(originalPath));

        var newPath = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(renamedPath));

        var affectedDocuments =
            DocumentsAtOrUnder(oldPath);

        foreach (var document in affectedDocuments)
        {
            string updatedPath;

            if (string.Equals(
                document.FilePath,
                oldPath,
                StringComparison.OrdinalIgnoreCase))
            {
                updatedPath = newPath;
            }
            else
            {
                var relative = Path.GetRelativePath(
                    oldPath,
                    document.FilePath);

                updatedPath = Path.Combine(
                    newPath,
                    relative);
            }

            document.UpdateLocation(
                updatedPath,
                RelativePathOf(updatedPath));
        }

        foreach (var document in affectedDocuments)
        {
            document.Reanalyze();
        }

        // Actualizar breadcrumb, título y documento activo
        OnSelectedDocumentChanged(SelectedDocument);
    }

    public void CloseDocumentsAtOrUnder(string path)
    {
        var affectedDocuments =
            DocumentsAtOrUnder(path);

        foreach (var document in affectedDocuments)
        {
            document.Close();
        }
    }

    private void CloseDocument(EditorDocumentViewModel document)
    {
        var index = Documents.IndexOf(document);
        if (index < 0)
        {
            return;
        }

        document.ContentChanged -= OnDocumentContentChanged;
        Documents.Remove(document);

        if (SelectedDocument == document)
        {
            SelectedDocument = Documents.Count == 0
                ? null
                : Documents[Math.Min(index, Documents.Count - 1)];
        }

        // Sin la pestaña, el compilador vuelve a leer ese archivo de disco.
        ReanalyzeDocumentsAffectedBy(document);
    }

    private void OnDocumentContentChanged(object? sender, EventArgs e)
    {
        if (sender is EditorDocumentViewModel document)
        {
            ReanalyzeDocumentsAffectedBy(document);
        }
    }

    // Los diagnósticos de un archivo dependen de los demás de su proyecto (la
    // misma carpeta, §6.6): al cambiar uno se vuelven a analizar los otros.
    private void ReanalyzeDocumentsAffectedBy(EditorDocumentViewModel changed)
    {
        var projectDirectory = Path.GetDirectoryName(changed.FilePath);

        var affected = Documents.Where(document =>
            document != changed &&
            string.Equals(Path.GetDirectoryName(document.FilePath), projectDirectory, StringComparison.OrdinalIgnoreCase));

        foreach (var document in affected)
        {
            document.Reanalyze();
        }
    }

    partial void OnSelectedDocumentChanged(EditorDocumentViewModel? value)
    {
        _messenger.Send(new ActiveDocumentChangedMessage(value));

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
