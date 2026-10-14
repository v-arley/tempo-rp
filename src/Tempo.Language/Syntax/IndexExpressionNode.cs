namespace Tempo.Language.Syntax;

/// <summary>
/// Representa el acceso indexado a una colección.
///
/// Ejemplos:
/// numeros[i]
/// matriz[fila][columna]
/// </summary>
public sealed record IndexExpressionNode(
    ExpressionNode Target,
    ExpressionNode Index,
    int Line,
    int Column
) : ExpressionNode;