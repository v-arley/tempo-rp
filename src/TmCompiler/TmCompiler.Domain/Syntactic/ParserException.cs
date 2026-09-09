using System.Text;

namespace TmCompiler.Domain.Syntactic;

/// <summary>
/// Excepción especializada para errores detectados durante
/// el análisis sintáctico del lenguaje TM.
/// Incluye ubicación del error para generar mensajes claros.
/// </summary>
public sealed class ParserException : Exception
{
    public string FileName { get; }
    public int Line { get; }
    public int Column { get; }

    public ParserException(
        string message,
        string fileName,
        int line,
        int column)
        : base(message)
    {
        FileName = fileName;
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Formatea el error según el estándar utilizado por TM.
    /// </summary>
    public string ToStandardFormat(
        string errorCode,
        string description,
        string suggestion)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"[ERROR {errorCode}] Error sintáctico");
        sb.AppendLine($"Archivo: {FileName}");
        sb.AppendLine($"Línea: {Line}");
        sb.AppendLine($"Descripción: {description}");
        sb.AppendLine($"Posible solución: {suggestion}");

        return sb.ToString();
    }
}