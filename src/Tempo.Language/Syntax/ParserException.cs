using Tempo.Language.Diagnostics;

namespace Tempo.Language.Syntax;

/// <summary>
/// Excepción especializada para errores detectados durante
/// el análisis sintáctico del lenguaje TM.
/// Incluye ubicación del error para generar mensajes claros.
/// </summary>
public sealed class ParserException : Exception
{
    public string Code { get; }
    public string FileName { get; }
    public int Line { get; }
    public int Column { get; }

    public ParserException(string code, string message, string fileName, int line, int column)
        : base(message)
    {
        Code = code;
        FileName = fileName;
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Formatea el error según el estándar utilizado por TM.
    /// </summary>
    public string ToStandardFormat(string suggestion)
    {
        var diagnostic = new Diagnostic(
            DiagnosticSeverity.Error,
            Code,
            DiagnosticCatalog.TitleOf(Code),
            FileName,
            Line,
            Message,
            suggestion);

        return diagnostic.ToStandardFormat();
    }
}