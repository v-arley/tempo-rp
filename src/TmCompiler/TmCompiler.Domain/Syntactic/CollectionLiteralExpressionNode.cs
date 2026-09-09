namespace TmCompiler.Domain.Syntactic;

/// <summary>
/// Representa un literal de colección TM.
/// Puede representar tanto un sequence como las filas de un score.
///
/// Ejemplos:
/// [1, 2, 3]
/// [[1, 2], [3, 4]]
/// </summary>
public sealed record CollectionLiteralExpressionNode(
    IReadOnlyList<ExpressionNode> Elements,
    int Line,
    int Column
) : ExpressionNode;