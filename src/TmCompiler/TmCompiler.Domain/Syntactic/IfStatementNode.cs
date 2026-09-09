namespace TmCompiler.Domain.Syntactic;

/// <summary>
/// Representa una sentencia condicional TM.
///
/// Ejemplos:
/// if [condicion] > ... <
/// if [condicion] > ... < else > ... <
/// if [condicion] > ... < else if [otra] > ... <
/// </summary>
public sealed record IfStatementNode(
    ExpressionNode Condition,
    BlockNode ThenBlock,
    IReadOnlyList<ElseIfClauseNode> ElseIfClauses,
    BlockNode ElseBlock,
    int Line,
    int Column
) : StatementNode;

/// <summary>
/// Representa una rama "else if".
/// </summary>
public sealed record ElseIfClauseNode(
    ExpressionNode Condition,
    BlockNode Body
) : AstNode;