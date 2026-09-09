using TmCompiler.Domain.Lexical;

namespace TmCompiler.Domain.Syntactic;

/// <summary>
/// Representa una expresión con un operador unario.
///
/// Ejemplos:
/// -10
/// -x
/// not activo
/// </summary>
public sealed record UnaryExpressionNode(
    TokenType Operator,
    ExpressionNode Operand,
    int Line,
    int Column
) : ExpressionNode;