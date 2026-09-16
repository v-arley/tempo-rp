namespace TmCompiler.Domain.Syntactic;

/// <summary>
/// Representa una rama "else if".
/// </summary>
public sealed record ElseIfClauseNode(
    ExpressionNode Condition,
    BlockNode Body
) : AstNode;
