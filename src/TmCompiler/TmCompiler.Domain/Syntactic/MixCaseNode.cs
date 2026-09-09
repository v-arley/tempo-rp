namespace TmCompiler.Domain.Syntactic;

/// <summary>
/// Representa un caso individual dentro de una sentencia mix.
///
/// Ejemplo:
/// case 1: >
///     output >> "Uno";
/// <
/// </summary>
public sealed record MixCaseNode(
    ExpressionNode Value,
    BlockNode Body
) : AstNode;