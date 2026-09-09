using TmCompiler.Domain.Lexical;

namespace TmCompiler.Domain.Syntactic;

/// <summary>
/// Representa la expresión de actualización de un ciclo loop.
///
/// Ejemplos:
/// i++
/// i--
/// i = i + 2
/// </summary>
public sealed record LoopUpdateNode(
    string Identifier,
    TokenType Operator,
    ExpressionNode Value,
    int Line,
    int Column
) : AstNode;