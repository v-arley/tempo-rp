using Tempo.Language.Diagnostics;
using Tempo.Language.Projects;
using Tempo.Language.Symbols;
using Tempo.Language.Syntax;

namespace Tempo.Language.Binding;

/// <summary>
/// Análisis semántico de un proyecto completo. Analiza cada archivo con
/// <see cref="SemanticAnalyzer"/> en orden de dependencias, entregándole las
/// firmas de los archivos que puede ver, y después aplica las comprobaciones
/// que solo tienen sentido sobre el proyecto entero: funciones duplicadas
/// entre archivos (E013) y funciones nunca invocadas (W003).
/// </summary>

public sealed class ProjectAnalyzer
{
    private readonly TempoProject _project;
    private readonly ImportGraph _imports;
    private readonly ProjectDiagnostics _diagnostics;
    private readonly Dictionary<SyntaxTree, List<FunctionSymbol>> _functionsDeclaredIn = new();

    public ProjectAnalyzer(TempoProject project, ImportGraph imports, ProjectDiagnostics diagnostics)
    {
        _project = project;
        _imports = imports;
        _diagnostics = diagnostics;
    }

    public void Analyze()
    {
        foreach (SyntaxTree file in _imports.InDependencyOrder.Where(file => file.IsParsed))
        {
            AnalyzeFile(file);
        }

        ReportFunctionsDeclaredInSeveralFiles();
        ReportUninvokedFunctions();
    }

    private void AnalyzeFile(SyntaxTree file)
    {
        IReadOnlyList<SyntaxTree> visibleFiles = _imports.VisibleFrom(file);
        HashSet<string> ownNames = FunctionNamesOf(file);

        // Una función propia con el mismo nombre que una importada se reporta
        // como duplicado de proyecto, no al sembrar las firmas.
        List<FunctionSymbol> importedFunctions = visibleFiles
            .Where(_functionsDeclaredIn.ContainsKey)
            .SelectMany(visible => _functionsDeclaredIn[visible])
            .Where(function => !ownNames.Contains(function.Name))
            .DistinctBy(function => function.Name)
            .ToList();

        var analyzer = new SemanticAnalyzer(file.Root, file.FileName, importedFunctions)
        {
            ReportsUninvokedFunctions = false,
            UnimportedFunctions = FunctionsOutside(file, visibleFiles)
        };

        SemanticAnalysisResult result = analyzer.Analyze();

        _diagnostics.Add(result.Diagnostics);
        _functionsDeclaredIn[file] = result.DeclaredFunctions.Except(importedFunctions).ToList();
    }

    /// <summary>
    /// Funciones declaradas en archivos que <paramref name="file"/> no puede ver,
    /// con el archivo que declara cada una.
    /// </summary>
    private Dictionary<string, string> FunctionsOutside(SyntaxTree file, IReadOnlyList<SyntaxTree> visibleFiles)
    {
        var outside = new Dictionary<string, string>(StringComparer.Ordinal);

        IEnumerable<SyntaxTree> hiddenFiles = _project.Files
            .Where(other => other != file && other.IsParsed && !visibleFiles.Contains(other));

        foreach (SyntaxTree hidden in hiddenFiles)
        {
            foreach (FunctionDeclarationNode function in hidden.Root.Functions)
            {
                outside.TryAdd(function.Identifier, hidden.FileName);
            }
        }

        return outside;
    }

    private void ReportFunctionsDeclaredInSeveralFiles()
    {
        var firstDeclaration = new Dictionary<string, (FunctionSymbol Function, SyntaxTree File)>(StringComparer.Ordinal);

        foreach (SyntaxTree file in _imports.InDependencyOrder.Where(_functionsDeclaredIn.ContainsKey))
        {
            foreach (FunctionSymbol function in _functionsDeclaredIn[file])
            {
                if (firstDeclaration.TryAdd(function.Name, (function, file)))
                {
                    continue;
                }

                (FunctionSymbol original, SyntaxTree originalFile) = firstDeclaration[function.Name];

                _diagnostics.In(file.FileName).ReportError(
                    DiagnosticCodes.FunctionOverload,
                    function.Line,
                    $"La función '{function.Name}' ya se declaró en '{originalFile.FileName}' " +
                    $"(línea {original.Line}).",
                    "Las funciones comparten un único espacio de nombres por proyecto: renombre una de ellas.");
            }
        }
    }

    private void ReportUninvokedFunctions()
    {
        foreach ((SyntaxTree file, List<FunctionSymbol> functions) in _functionsDeclaredIn)
        {
            foreach (FunctionSymbol function in functions.Where(function => !function.IsInvoked))
            {
                _diagnostics.In(file.FileName).ReportWarning(
                    DiagnosticCodes.UnusedFunction,
                    function.Line,
                    $"La función '{function.Name}' se declara pero nunca se invoca.",
                    "Elimínela o invóquela desde 'session' u otra función.");
            }
        }
    }

    private static HashSet<string> FunctionNamesOf(SyntaxTree file)
    {
        return file.Root.Functions
            .Select(function => function.Identifier)
            .ToHashSet(StringComparer.Ordinal);
    }
}
