using TmCompiler.Domain.Syntactic;

namespace TmCompiler.Application.Parsing;

/// <summary>
/// Define el contrato del analizador sintáctico de TM.
/// El Parser recibe una secuencia de tokens ya generada por el Lexer
/// y construye el árbol de sintaxis abstracta (AST).
/// </summary>
public interface IParser
{
    /// <summary>
    /// Ejecuta el análisis sintáctico completo y retorna
    /// el nodo raíz del AST.
    /// </summary>
    /// <exception cref="ParserException">
    /// Se lanza cuando la secuencia de tokens no cumple
    /// con la gramática sintáctica de TM.
    /// </exception>
    ProgramNode Parse();
}