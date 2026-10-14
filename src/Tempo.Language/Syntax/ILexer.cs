namespace Tempo.Language.Syntax;

/// <summary>
/// Define el contrato del analizador léxico. Aplica el principio de
/// Inversión de Dependencias (DIP): los consumidores (parser, IDE, etc.)
/// dependen de esta abstracción, no de la implementación concreta <see cref="Lexer"/>.
/// </summary>

public interface ILexer
{
    /// <summary>
    /// Ejecuta el análisis léxico completo sobre la fuente y retorna
    /// la secuencia de tokens reconocidos, incluyendo <see cref="TokenType.EndOfFile"/>.
    /// </summary>
    
    /// <exception cref="LexerException">
    /// Se lanza cuando se detecta un carácter inválido, string no cerrado,
    /// comentario de bloque sin cierre, u otra anomalía léxica.
    /// </exception>
    
    IEnumerable<Token> Tokenize();
}