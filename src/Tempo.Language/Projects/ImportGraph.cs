using Tempo.Language.Diagnostics;
using Tempo.Language.Syntax;

namespace Tempo.Language.Projects;

/// <summary>
/// Grafo de importaciones del proyecto (§6.5). Al construirse resuelve cada
/// <c>import</c> contra el disco (E014) y recorre el grafo en profundidad: así
/// detecta los ciclos (E012) y deja cada archivo después de los que importa,
/// que es el orden en que la semántica debe analizarlos.
/// </summary>

public sealed class ImportGraph
{
    private readonly Dictionary<SyntaxTree, List<(ImportNode Statement, SyntaxTree Target)>> _importsByFile = new();
    private readonly List<SyntaxTree> _dependencyOrder = new();

    private ImportGraph()
    {
    }

    /// <summary>Archivos ordenados de modo que cada uno aparece después de los que importa.</summary>
    public IReadOnlyList<SyntaxTree> InDependencyOrder => _dependencyOrder;

    public static ImportGraph Resolve(TempoProject project, ProjectDiagnostics diagnostics)
    {
        var graph = new ImportGraph();

        // Resolver un import puede cargar un archivo nuevo, que también se recorre.
        for (int index = 0; index < project.Files.Count; index++)
        {
            graph.ResolveImportsOf(project.Files[index], project, diagnostics);
        }

        var visiting = new HashSet<SyntaxTree>();
        var visited = new HashSet<SyntaxTree>();
        var path = new List<SyntaxTree>();

        IEnumerable<SyntaxTree> roots = project.Files.OrderBy(file => file == project.EntryFile ? 0 : 1);

        foreach (SyntaxTree file in roots)
        {
            graph.Visit(file, visiting, visited, path, diagnostics);
        }

        return graph;
    }

    /// <summary>
    /// Archivos cuyo contenido es visible desde <paramref name="file"/>: los que
    /// importa directamente y, como la inclusión es textual, los que importan
    /// ellos a su vez.
    /// </summary>
    public IReadOnlyList<SyntaxTree> VisibleFrom(SyntaxTree file)
    {
        var visible = new List<SyntaxTree>();
        var pending = new Queue<SyntaxTree>(ImportsOf(file));

        while (pending.Count > 0)
        {
            SyntaxTree next = pending.Dequeue();

            if (next == file || visible.Contains(next))
            {
                continue;
            }

            visible.Add(next);

            foreach (SyntaxTree imported in ImportsOf(next))
            {
                pending.Enqueue(imported);
            }
        }

        return visible;
    }

    private IEnumerable<SyntaxTree> ImportsOf(SyntaxTree file)
    {
        return _importsByFile.TryGetValue(file, out var imports)
            ? imports.Select(import => import.Target)
            : Enumerable.Empty<SyntaxTree>();
    }

    private void ResolveImportsOf(SyntaxTree file, TempoProject project, ProjectDiagnostics diagnostics)
    {
        var imports = new List<(ImportNode Statement, SyntaxTree Target)>();
        _importsByFile[file] = imports;

        if (!file.IsParsed)
        {
            return;
        }

        foreach (ImportNode import in file.Root.Imports)
        {
            string fullPath = Path.GetFullPath(Path.Combine(file.DirectoryPath, import.FilePath));
            SyntaxTree target = project.FindOrLoad(fullPath);

            if (target is null)
            {
                diagnostics.In(file.FileName).ReportError(
                    DiagnosticCodes.ImportNotFound,
                    import.Line,
                    $"No se encontró el archivo importado '{import.FilePath}'.",
                    "Revise el nombre del archivo y que la ruta sea relativa al archivo que lo importa.");

                continue;
            }

            imports.Add((import, target));
        }
    }

    private void Visit(
        SyntaxTree file,
        HashSet<SyntaxTree> visiting,
        HashSet<SyntaxTree> visited,
        List<SyntaxTree> path,
        ProjectDiagnostics diagnostics)
    {
        if (visited.Contains(file))
        {
            return;
        }

        visiting.Add(file);
        path.Add(file);

        foreach ((ImportNode statement, SyntaxTree target) in _importsByFile[file])
        {
            if (visiting.Contains(target))
            {
                ReportCycle(file, statement, target, path, diagnostics);
                continue;
            }

            Visit(target, visiting, visited, path, diagnostics);
        }

        path.RemoveAt(path.Count - 1);
        visiting.Remove(file);
        visited.Add(file);
        _dependencyOrder.Add(file);
    }

    private static void ReportCycle(
        SyntaxTree file,
        ImportNode statement,
        SyntaxTree target,
        List<SyntaxTree> path,
        ProjectDiagnostics diagnostics)
    {
        IEnumerable<string> cycle = path
            .SkipWhile(step => step != target)
            .Append(target)
            .Select(step => step.FileName);

        diagnostics.In(file.FileName).ReportError(
            DiagnosticCodes.ImportCycle,
            statement.Line,
            $"Importación cíclica detectada entre '{file.FileName}' y '{target.FileName}': " +
            $"{string.Join(" -> ", cycle)}.",
            "Mueva las funciones compartidas a un módulo que no importe a ninguno de los dos.");
    }
}
