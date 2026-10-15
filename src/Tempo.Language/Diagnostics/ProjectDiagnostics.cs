namespace Tempo.Language.Diagnostics;

/// <summary>
/// Diagnósticos de todos los archivos de un proyecto. Cada fase reporta en el
/// archivo que corresponde con <see cref="In"/>, o entrega los diagnósticos que
/// ya produjo con <see cref="Add"/>. <see cref="All"/> los devuelve ordenados por
/// archivo y línea.
/// </summary>

public sealed class ProjectDiagnostics
{
    private readonly Dictionary<string, DiagnosticBag> _bagsByFile = new(StringComparer.Ordinal);
    private readonly List<Diagnostic> _added = new();

    public IReadOnlyList<Diagnostic> All =>
        _added
            .Concat(_bagsByFile.Values.SelectMany(bag => bag.Diagnostics))
            .OrderBy(diagnostic => diagnostic.FileName, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Line)
            .ToList();

    public DiagnosticBag In(string fileName)
    {
        if (!_bagsByFile.TryGetValue(fileName, out DiagnosticBag bag))
        {
            bag = new DiagnosticBag(fileName);
            _bagsByFile.Add(fileName, bag);
        }

        return bag;
    }

    public void Add(IEnumerable<Diagnostic> diagnostics)
    {
        _added.AddRange(diagnostics);
    }
}
