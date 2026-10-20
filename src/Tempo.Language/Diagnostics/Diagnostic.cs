using System.Text;

namespace Tempo.Language.Diagnostics;

/// <summary>
/// Diagnóstico emitido por el análisis semántico. A diferencia de
/// <see cref="Lexical.LexerException"/> y <see cref="Syntactic.ParserException"/>,
/// no es una excepción: el analizador acumula diagnósticos y continúa, porque
/// el AST ya está completo y las advertencias no impiden la ejecución.
/// El formato de salida es el mismo bloque de cinco campos de la §7.1.
/// </summary>

public sealed record Diagnostic(
    DiagnosticSeverity Severity,
    string Code,
    string Title,
    string FileName,
    int Line,
    string Description,
    string Suggestion)
{
    /// <summary>
    /// Columna donde empieza el error, o <c>null</c> cuando la fase solo conoce
    /// la línea (análisis semántico). No forma parte del formato de la §7.1; la
    /// usan los hosts que subrayan el texto, como el IDE.
    /// </summary>
    public int? Column { get; init; }

    public bool IsError => Severity == DiagnosticSeverity.Error;

    /// <summary>
    ///     Formatea el diagnóstico según el estándar de la especificación TM v1.1:
    ///     [ERROR E###] Título        (o [ADVERTENCIA W###] Título)
    ///     Archivo: nombre
    ///     Línea: número
    ///     Descripción: detalle
    ///     Posible solución: sugerencia
    /// </summary>

    public string ToStandardFormat()
    {
        string label = Severity == DiagnosticSeverity.Warning ? "ADVERTENCIA" : "ERROR";

        var sb = new StringBuilder();
        sb.AppendLine($"[{label} {Code}] {Title}");
        sb.AppendLine($"Archivo: {FileName}");
        sb.AppendLine($"Línea: {Line}");
        sb.AppendLine($"Descripción: {Description}");
        sb.AppendLine($"Posible solución: {Suggestion}");
        return sb.ToString();
    }
}
