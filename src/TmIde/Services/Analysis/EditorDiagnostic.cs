namespace TmIde.Services.Analysis;

/// <summary>
/// Diagnóstico listo para mostrar en el editor. <see cref="Column"/> es nulo
/// cuando la fase del compilador solo informa la línea (análisis semántico);
/// entonces se subraya la línea completa.
/// </summary>

public sealed record EditorDiagnostic(
    EditorDiagnosticSeverity Severity,
    string Code,
    string Title,
    string Description,
    string Suggestion,
    int Line,
    int? Column);
