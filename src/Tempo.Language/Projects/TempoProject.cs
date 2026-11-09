using Tempo.Language.Syntax;

namespace Tempo.Language.Projects;

/// <summary>
/// Conjunto de archivos que se compilan juntos (§6.6). Se abre de dos formas:
/// <list type="bullet">
///   <item><see cref="FromDirectory"/>: todos los <c>.tm</c> y <c>.tmpr</c> del
///   directorio forman el proyecto, y se exige exactamente un <c>.tmpr</c>.</item>
///   <item><see cref="FromFile"/>: el archivo indicado es la entrada, y el
///   proyecto se completa con los archivos que importa.</item>
/// </list>
/// Los documentos abiertos (ruta completa → texto) sustituyen al contenido en
/// disco, y los que todavía no se guardaron también forman parte del proyecto.
/// Es lo que permite a un editor compilar lo que el usuario ve, no lo último
/// que guardó.
/// </summary>

public sealed class TempoProject
{
    private const string SourcePattern = "*.tm*";

    private static readonly IReadOnlyDictionary<string, string> NoOpenDocuments =
        new Dictionary<string, string>();

    private readonly List<SyntaxTree> _files = new();
    private readonly Dictionary<string, SyntaxTree> _filesByPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _openDocuments;

    private TempoProject(
        string rootDirectory,
        bool isWholeDirectory,
        IReadOnlyDictionary<string, string> openDocuments)
    {
        RootDirectory = rootDirectory;
        IsWholeDirectory = isWholeDirectory;
        _openDocuments = (openDocuments ?? NoOpenDocuments)
            .ToDictionary(
                document => Path.GetFullPath(document.Key),
                document => document.Value,
                StringComparer.OrdinalIgnoreCase);
    }

    public string RootDirectory { get; }

    /// <summary><c>true</c> si se abrió desde un directorio y se aplican las reglas de §6.6.</summary>
    public bool IsWholeDirectory { get; }

    public IReadOnlyList<SyntaxTree> Files => _files;

    /// <summary>Archivo desde el que arranca la ejecución, o <c>null</c> si el proyecto no tiene ninguno.</summary>
    public SyntaxTree EntryFile { get; private set; }

    public IEnumerable<SyntaxTree> ProjectFiles => _files.Where(file => file.IsProjectFile);

    public IEnumerable<SyntaxTree> Modules => _files.Where(file => file.IsModule);

    public static TempoProject Open(string path, IReadOnlyDictionary<string, string> openDocuments = null)
    {
        return Directory.Exists(path)
            ? FromDirectory(path, openDocuments)
            : FromFile(path, openDocuments);
    }

    public static TempoProject FromDirectory(string directory, IReadOnlyDictionary<string, string> openDocuments = null)
    {
        var project = new TempoProject(Path.GetFullPath(directory), isWholeDirectory: true, openDocuments);

        IEnumerable<string> savedSources = Directory
            .EnumerateFiles(project.RootDirectory, SourcePattern)
            .Where(IsSourceFile);

        IEnumerable<string> unsavedSources = project._openDocuments.Keys
            .Where(path => IsSourceFile(path) && IsInDirectory(path, project.RootDirectory));

        IEnumerable<string> sources = savedSources
            .Concat(unsavedSources)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

        foreach (string source in sources)
        {
            project.Add(source);
        }

        project.EntryFile = project.ProjectFiles.FirstOrDefault();
        return project;
    }

    public static TempoProject FromFile(string filePath, IReadOnlyDictionary<string, string> openDocuments = null)
    {
        string fullPath = Path.GetFullPath(filePath);
        var project = new TempoProject(Path.GetDirectoryName(fullPath), isWholeDirectory: false, openDocuments);

        project.EntryFile = project.Add(fullPath);
        return project;
    }

    /// <summary>
    /// Devuelve el archivo con esa ruta, cargándolo si todavía no forma parte del
    /// proyecto. Devuelve <c>null</c> si no existe ni en disco ni entre los
    /// documentos abiertos.
    /// </summary>
    public SyntaxTree FindOrLoad(string fullPath)
    {
        if (_filesByPath.TryGetValue(fullPath, out SyntaxTree existing))
        {
            return existing;
        }

        return _openDocuments.ContainsKey(fullPath) || File.Exists(fullPath) ? Add(fullPath) : null;
    }

    /// <summary>Archivo del proyecto con esa ruta, o <c>null</c> si no forma parte de él.</summary>
    public SyntaxTree FileAt(string path)
    {
        _filesByPath.TryGetValue(Path.GetFullPath(path), out SyntaxTree file);
        return file;
    }

    private static bool IsSourceFile(string path)
    {
        string extension = Path.GetExtension(path);

        return extension.Equals(".tm", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(SyntaxTree.ProjectFileExtension, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInDirectory(string path, string directory)
    {
        return string.Equals(Path.GetDirectoryName(path), directory, StringComparison.OrdinalIgnoreCase);
    }

    private SyntaxTree Add(string fullPath)
    {
        string fileName = Path.GetRelativePath(RootDirectory, fullPath);

        SyntaxTree file = _openDocuments.TryGetValue(fullPath, out string openText)
            ? SyntaxTree.Parse(openText, fullPath, fileName)
            : SyntaxTree.Load(fullPath, fileName);

        _files.Add(file);
        _filesByPath.Add(fullPath, file);
        return file;
    }
}
