using Tempo.Language.Diagnostics;
using Tempo.Language.Evaluation;
using Xunit;

namespace Tempo.Language.Tests.Projects;

/// <summary>
/// Proyecto TM escrito en un directorio temporal, para probar la compilación
/// multiarchivo contra el disco real. Se declara con los archivos que contiene
/// y se borra al desecharse:
/// <code>
/// using var project = ProjectWorkspace.With(
///     ("principal.tmpr", "..."),
///     ("utilidades.tm", "..."));
/// </code>
/// </summary>

public sealed class ProjectWorkspace : IDisposable
{
    private ProjectWorkspace(string directory)
    {
        Directory = directory;
    }

    public string Directory { get; }

    public static ProjectWorkspace With(params (string FileName, string Source)[] files)
    {
        string directory = Path.Combine(Path.GetTempPath(), "tempo-tests", Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(directory);

        foreach ((string fileName, string source) in files)
        {
            File.WriteAllText(Path.Combine(directory, fileName), source);
        }

        return new ProjectWorkspace(directory);
    }

    public string PathOf(string fileName)
    {
        return Path.Combine(Directory, fileName);
    }

    /// <summary>Compila el directorio completo como un solo proyecto.</summary>
    public Compilation CompileDirectory()
    {
        return Compilation.FromPath(Directory);
    }

    /// <summary>Compila un archivo y los que importa.</summary>
    public Compilation CompileFile(string fileName)
    {
        return Compilation.FromPath(PathOf(fileName));
    }

    /// <summary>
    /// Compila el directorio con documentos abiertos en un editor: su texto
    /// sustituye al del disco, y pueden no existir en disco todavía.
    /// </summary>
    public Compilation CompileDirectoryWithOpen(params (string FileName, string Text)[] openDocuments)
    {
        Dictionary<string, string> texts = openDocuments.ToDictionary(
            document => PathOf(document.FileName),
            document => document.Text);

        return Compilation.FromPath(Directory, texts);
    }

    /// <summary>
    /// Compila el directorio, lo ejecuta y devuelve lo que escribió el programa.
    /// </summary>
    public string RunDirectory(IRuntimePulse? pulse = null)
    {
        Compilation compilation = CompileDirectory();

        Assert.True(compilation.IsExecutable, Describe(compilation.Diagnostics));

        var output = new CapturedOutput();
        compilation.Run(new NoInput(), output, pulse ?? NullRuntimePulse.Instance);
        return output.Text;
    }

    public void Dispose()
    {
        System.IO.Directory.Delete(Directory, recursive: true);
    }

    private static string Describe(IEnumerable<Diagnostic> diagnostics)
    {
        return string.Join(Environment.NewLine, diagnostics.Select(diagnostic => diagnostic.ToStandardFormat()));
    }

    private sealed class NoInput : IRuntimeInput
    {
        public string? ReadLine()
        {
            return null;
        }
    }

    private sealed class CapturedOutput : IRuntimeOutput
    {
        public string Text { get; private set; } = string.Empty;

        public void Write(string text)
        {
            Text += text;
        }
    }
}
