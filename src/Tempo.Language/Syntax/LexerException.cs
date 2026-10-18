using Tempo.Language.Diagnostics;

namespace Tempo.Language.Syntax;

/// <summary>
/// Excepción especializada para errores detectados durante el análisis léxico.
/// Incluye metadatos de ubicación (línea, columna, archivo) para generar
/// mensajes de error estandarizados según la Sección 7 de la especificación TM.
/// </summary>

public sealed class LexerException : Exception
{
    public string Code { get; }
    public string FileName { get; }
    public int Line { get; }
    public int Column { get; }

   
    public LexerException(string code, string message, string fileName, int line, int column)
        : base(message)
    {
        Code = code;
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