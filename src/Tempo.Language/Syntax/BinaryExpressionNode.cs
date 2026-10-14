namespace Tempo.Language.Syntax;

/// <summary>
/// Representa una expresión formada por dos operandos
/// y un operador binario.
///
/// Ejemplos:
/// 10 + 20
/// a * b
/// x > 5
/// activo and disponible
/// </summary>
public sealed record BinaryExpressionNode(
    ExpressionNode Left,
    TokenType Operator,
    ExpressionNode Right,
    int Line,
    int Column
) : ExpressionNode;