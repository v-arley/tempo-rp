namespace TmCompiler.Domain.Lexical;

/// <summary>
/// Representación inmutable y ligera de un token del lenguaje TM.
/// Se utiliza <c>readonly record struct</c> para minimizar asignaciones
/// de memoria en el heap durante el análisis léxico de grandes fuentes,
/// aprovechando el paso por valor y la inmutabilidad.
/// </summary>

/// <param name="TokenType">Categoría semántica del token.</param>
/// <param name="Lexeme">Texto literal del fuente.</param>
/// <param name="Line">Línea de origen.</param>
/// <param name="Column">Columna de origen.</param>

public readonly record struct Token(TokenType TokenType, string Lexeme, int Line, int Column) : IToken
{
    // Representación legible para depuración y trazas del compilador
    public override string ToString() =>
    $"[{TokenType}] '{Lexeme}' (L{Line}, C{Column})";
}