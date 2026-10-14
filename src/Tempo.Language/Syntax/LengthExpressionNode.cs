namespace Tempo.Language.Syntax;

/// <summary>
/// Representa el acceso a la propiedad length() de una colección.
///
/// Ejemplos:
/// numeros.length()
/// matriz.length()
/// </summary>
public sealed record LengthExpressionNode(
    ExpressionNode Target,
    int Line,
    int Column
) : ExpressionNode;