namespace TmCompiler.Domain.Syntactic;

/// <summary>
/// Representa una asignación de valor a una variable.
///
/// Ejemplo:
/// x = 20;
/// </summary>
public sealed record AssignmentStatementNode(
    string Identifier,
    ExpressionNode Value,
    int Line,
    int Column
) : StatementNode;