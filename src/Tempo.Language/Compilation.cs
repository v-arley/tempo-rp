using Tempo.Language.Binding;
using Tempo.Language.Diagnostics;
using Tempo.Language.Evaluation;
using Tempo.Language.Projects;
using Tempo.Language.Syntax;

namespace Tempo.Language;

/// <summary>
/// Fachada del compilador: recibe un proyecto, ejecuta todas las fases de
/// análisis y expone el resultado. Los hosts (la CLI, el IDE) solo necesitan
/// esta clase:
/// <code>
/// Compilation compilation = Compilation.FromPath(ruta);
/// if (compilation.IsExecutable) compilation.Run(entrada, salida);
/// </code>
/// Las fases se encadenan en orden fijo: importaciones → errores de sintaxis
/// → reglas de proyecto → semántica.
/// </summary>

public sealed class Compilation
{
    private readonly ImportGraph _imports;

    private Compilation(TempoProject project, ImportGraph imports, IReadOnlyList<Diagnostic> diagnostics)
    {
        Project = project;
        _imports = imports;
        Diagnostics = diagnostics;
    }

    public TempoProject Project { get; }

    /// <summary>Diagnósticos de todos los archivos, ordenados por archivo y línea.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.IsError);

    /// <summary><c>true</c> si no hay errores y el archivo de entrada es un <c>.tmpr</c> con <c>session</c>.</summary>
    public bool IsExecutable =>
        !HasErrors &&
        Project.EntryFile is { IsProjectFile: true, IsParsed: true } entry &&
        entry.Root.Session is not null;

    public static Compilation FromPath(string path)
    {
        return Create(TempoProject.Open(path));
    }

    public static Compilation Create(TempoProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        var diagnostics = new ProjectDiagnostics();

        // Resolver las importaciones primero: pueden sumar archivos al proyecto.
        ImportGraph imports = ImportGraph.Resolve(project, diagnostics);

        foreach (SyntaxTree file in project.Files)
        {
            diagnostics.Add(file.Diagnostics);
        }

        foreach (IProjectRule rule in ProjectRules.All)
        {
            rule.Check(project, diagnostics);
        }

        new ProjectAnalyzer(project, imports, diagnostics).Analyze();

        return new Compilation(project, imports, diagnostics.All);
    }

    public IEnumerable<Diagnostic> DiagnosticsOf(SyntaxTree file)
    {
        return Diagnostics.Where(diagnostic => diagnostic.FileName == file.FileName);
    }

    /// <summary>
    /// Ejecuta el proyecto. La importación es textual (§6.5): el programa que se
    /// ejecuta es el archivo de entrada junto con las funciones de todos los
    /// archivos que importa.
    /// </summary>
    public void Run(IRuntimeInput input, IRuntimeOutput output)
    {
        if (!IsExecutable)
        {
            throw new InvalidOperationException(
                "Solo se puede ejecutar un proyecto sin errores cuyo archivo de entrada sea un .tmpr con session.");
        }

        ProgramNode program = EntryProgramWithImports();

        var sourceFiles =
            new Dictionary<string, string>(StringComparer.Ordinal);

        SyntaxTree entry = Project.EntryFile;

        foreach (SyntaxTree file in _imports.VisibleFrom(entry).Append(entry))
        {
            foreach (FunctionDeclarationNode function in file.Root.Functions)
            {
                sourceFiles.TryAdd(
                    function.Identifier,
                    file.FileName);
            }
        }

        new Interpreter(
            program,
            input,
            output,
            sourceFiles,
            entry.FileName).Execute();
    }

    private ProgramNode EntryProgramWithImports()
    {
        SyntaxTree entry = Project.EntryFile;

        List<FunctionDeclarationNode> functions = _imports.VisibleFrom(entry)
            .SelectMany(imported => imported.Root.Functions)
            .Concat(entry.Root.Functions)
            .ToList();

        return entry.Root with { Functions = functions };
    }
}
