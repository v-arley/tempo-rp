namespace Tempo.Language.Syntax;

/// <summary>
/// Representa un valor literal escrito directamente en el código.
/// Ejemplos: 10, 3.14, "Hola", 'A', true, false.
/// </summary>
public sealed record LiteralExpressionNode(
    TokenType LiteralType,
    string Value,
    int Line,
    int Column
) : ExpressionNode;