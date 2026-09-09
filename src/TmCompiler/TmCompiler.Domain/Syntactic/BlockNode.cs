namespace TmCompiler.Domain.Syntactic;

/// <summary>
/// Representa un bloque de código TM delimitado por > y <.
/// </summary>
public sealed record BlockNode(
    IReadOnlyList<StatementNode> Statements
) : AstNode;