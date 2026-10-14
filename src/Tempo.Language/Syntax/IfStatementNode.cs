namespace Tempo.Language.Syntax;

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