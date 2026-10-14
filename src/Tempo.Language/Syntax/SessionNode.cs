namespace Tempo.Language.Syntax;

/// <summary>
/// Representa la sentencia session del lenguaje TM.
/// </summary>
public sealed record SessionNode(
    BlockNode Body
) : AstNode;
