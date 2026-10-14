namespace Tempo.Language.Syntax;

/// <summary>
/// Contrato que define la estructura mínima de un token reconocido por el lexer.
/// Aplica el principio de Segregación de Interfaces (ISP) y Abstracción,
/// permitiendo que consumidores del lexer dependan de esta abstracción
/// en lugar de implementaciones concretas.
/// </summary>

public interface IToken
{
    // Tipo categórico del token dentro de la gramática de TM
    TokenType TokenType { get; }
    // Valor léxico textual exacto extraído del fuente
    string Lexeme { get; }
    // Línea física donde aparece el token (base 1)
    int Line { get; }
    // Columna inicial del token dentro de la línea (base 1)
    int Column { get; }
}