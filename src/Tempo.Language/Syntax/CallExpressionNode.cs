namespace Tempo.Language.Syntax;

/// <summary>
/// Representa una llamada a función utilizada como expresión.
///
/// Ejemplos:
/// sumar(2, 3)
/// fibonacci(n - 1)
/// saludar("Ana")
/// </summary>
public sealed record CallExpressionNode(
    string Identifier,
    IReadOnlyList<ExpressionNode> Arguments,
    int Line,
    int Column
) : ExpressionNode;