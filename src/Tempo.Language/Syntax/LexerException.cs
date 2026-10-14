using System.Text;

namespace Tempo.Language.Syntax;

/// <summary>
/// Excepción especializada para errores detectados durante el análisis léxico.
/// Incluye metadatos de ubicación (línea, columna, archivo) para generar
/// mensajes de error estandarizados según la Sección 7 de la especificación TM.
/// </summary>

public sealed class LexerException : Exception
{
    public string FileName { get; }
    public int Line { get; }
    public int Column { get; }

    public LexerException(string message, string fileName, int line, int column)
        : base(message)
    {
        FileName = fileName;
        Line = line;
        Column = column;
    }

    /// <summary>
    ///     Formatea el error según el estándar definido en la especificación TM v1.1:
    ///     [ERROR E###] Título
    ///     Archivo: nombre
    ///     Línea: número
    ///     Descripción: detalle
    ///     Posible solución: sugerencia
    /// </summary>
    
    public string ToStandardFormat(string errorCode, string description, string suggestion)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[ERROR {errorCode}] Error léxico");
        sb.AppendLine($"Archivo: {FileName}");
        sb.AppendLine($"Línea: {Line}");
        sb.AppendLine($"Descripción: {description}");
        sb.AppendLine($"Posible solución: {suggestion}");
        return sb.ToString();
    }
}