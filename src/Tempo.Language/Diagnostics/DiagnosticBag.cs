using System.Collections;

namespace Tempo.Language.Diagnostics;

/// <summary>
/// Acumulador de diagnósticos del análisis semántico. El nombre de archivo se
/// inyecta una sola vez, y los duplicados exactos se descartan para que un nodo
/// visitado dos veces no reporte lo mismo repetido.
/// </summary>

public sealed class DiagnosticBag : IReadOnlyCollection<Diagnostic>
{
    private readonly List<Diagnostic> _diagnostics = new();
    private readonly HashSet<string> _emitted = new(StringComparer.Ordinal);
    private readonly string _fileName;

    public DiagnosticBag(string fileName)
    {
        _fileName = fileName ?? "<unknown>";
    }

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

    public int Count => _diagnostics.Count;

    public bool HasErrors => _diagnostics.Exists(diagnostic => diagnostic.IsError);

    public int ErrorCount => _diagnostics.Count(diagnostic => diagnostic.IsError);

    public int WarningCount => _diagnostics.Count - ErrorCount;

    public void ReportError(string code, int line, string description, string suggestion)
    {
        Report(DiagnosticSeverity.Error, code, line, description, suggestion);
    }

    public void ReportWarning(string code, int line, string description, string suggestion)
    {
        Report(DiagnosticSeverity.Warning, code, line, description, suggestion);
    }

    public IEnumerator<Diagnostic> GetEnumerator()
    {
        return _diagnostics.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private void Report(
        DiagnosticSeverity severity,
        string code,
        int line,
        string description,
        string suggestion)
    {
        if (!_emitted.Add($"{code}|{line}|{description}"))
        {
            return;
        }

        _diagnostics.Add(
            new Diagnostic(
                severity,
                code,
                DiagnosticCatalog.TitleOf(code),
                _fileName,
                line,
                description,
                suggestion));
    }
}
