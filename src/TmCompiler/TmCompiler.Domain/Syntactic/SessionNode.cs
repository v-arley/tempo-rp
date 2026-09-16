namespace TmCompiler.Domain.Syntactic;

/// <summary>
/// Representa la sentencia session del lenguaje TM.
/// </summary>
public sealed record SessionNode(
    BlockNode Body
) : AstNode;
