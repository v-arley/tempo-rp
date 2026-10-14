namespace Tempo.Language.Syntax;

/// <summary>
/// Representa una rama "else if".
/// </summary>
public sealed record ElseIfClauseNode(
    ExpressionNode Condition,
    BlockNode Body
) : AstNode;
