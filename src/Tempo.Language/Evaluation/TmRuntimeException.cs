using Tempo.Language.Diagnostics;

namespace Tempo.Language.Evaluation;

/// <summary>
/// Excepción producida durante la ejecución de un programa TM.
/// </summary>
public sealed class TmRuntimeException : Exception
{
    public TmRuntimeException(
        string code,
        int line,
        string message)
        : base(message)
    {
        Code = code;
        Line = line;
    }

    public string Code { get; }

    public int Line { get; }

    public string? FileName { get; private set; }

    /// <summary>
    /// Convierte el error en un <see cref="Diagnostic"/> con el formato de la
    /// §7.1: título del catálogo y sugerencia de <see cref="RuntimeErrorSuggestions"/>.
    /// <paramref name="defaultFileName"/> se usa cuando el error no ocurrió dentro
    /// de una función con archivo conocido (por ejemplo, en <c>session</c>).
    /// </summary>
    public Diagnostic ToDiagnostic(string defaultFileName)
    {
        return new Diagnostic(
            DiagnosticSeverity.Error,
            Code,
            DiagnosticCatalog.TitleOf(Code),
            FileName ?? defaultFileName,
            Line,
            Message,
            RuntimeErrorSuggestions.For(Code));
    }

    internal void SetFileNameIfMissing(string? fileName)
    {
        if (FileName is null &&
            !string.IsNullOrWhiteSpace(fileName))
        {
            FileName = fileName;
        }
    }
}