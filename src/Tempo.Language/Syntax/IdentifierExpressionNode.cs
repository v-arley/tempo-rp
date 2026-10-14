namespace Tempo.Language.Syntax;

/// <summary>
/// Representa el uso de un identificador dentro de una expresión.
///
/// Ejemplo:
/// sample resultado = x;
///                    ^
/// </summary>
public sealed record IdentifierExpressionNode(
    string Name,
    int Line,
    int Column
) : ExpressionNode;