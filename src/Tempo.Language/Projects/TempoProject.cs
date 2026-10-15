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
/// </summary>

public sealed class TempoProject
{
    private const string SourcePattern = "*.tm*";

    private readonly List<SyntaxTree> _files = new();
    private readonly Dictionary<string, SyntaxTree> _filesByPath = new(StringComparer.OrdinalIgnoreCase);

    private TempoProject(string rootDirectory, bool isWholeDirectory)
    {
        RootDirectory = rootDirectory;
        IsWholeDirectory = isWholeDirectory;
    }

    public string RootDirectory { get; }

    /// <summary><c>true</c> si se abrió desde un directorio y se aplican las reglas de §6.6.</summary>
    public bool IsWholeDirectory { get; }

    public IReadOnlyList<SyntaxTree> Files => _files;

    /// <summary>Archivo desde el que arranca la ejecución, o <c>null</c> si el proyecto no tiene ninguno.</summary>
    public SyntaxTree EntryFile { get; private set; }

    public IEnumerable<SyntaxTree> ProjectFiles => _files.Where(file => file.IsProjectFile);

    public IEnumerable<SyntaxTree> Modules => _files.Where(file => file.IsModule);

    public static TempoProject Open(string path)
    {
        return Directory.Exists(path) ? FromDirectory(path) : FromFile(path);
    }

    public static TempoProject FromDirectory(string directory)
    {
        var project = new TempoProject(Path.GetFullPath(directory), isWholeDirectory: true);

        IEnumerable<string> sources = Directory
            .EnumerateFiles(project.RootDirectory, SourcePattern)
            .Where(IsSourceFile)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

        foreach (string source in sources)
        {
            project.Add(source);
        }

        project.EntryFile = project.ProjectFiles.FirstOrDefault();
        return project;
    }

    public static TempoProject FromFile(string filePath)
    {
        string fullPath = Path.GetFullPath(filePath);
        var project = new TempoProject(Path.GetDirectoryName(fullPath), isWholeDirectory: false);

        project.EntryFile = project.Add(fullPath);
        return project;
    }

    /// <summary>
    /// Devuelve el archivo con esa ruta, cargándolo si todavía no forma parte del
    /// proyecto. Devuelve <c>null</c> si el archivo no existe.
    /// </summary>
    public SyntaxTree FindOrLoad(string fullPath)
    {
        if (_filesByPath.TryGetValue(fullPath, out SyntaxTree existing))
        {
            return existing;
        }

        return File.Exists(fullPath) ? Add(fullPath) : null;
    }

    private static bool IsSourceFile(string path)
    {
        string extension = Path.GetExtension(path);

        return extension.Equals(".tm", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(SyntaxTree.ProjectFileExtension, StringComparison.OrdinalIgnoreCase);
    }

    private SyntaxTree Add(string fullPath)
    {
        string fileName = Path.GetRelativePath(RootDirectory, fullPath);
        SyntaxTree file = SyntaxTree.Load(fullPath, fileName);

        _files.Add(file);
        _filesByPath.Add(fullPath, file);
        return file;
    }
}
