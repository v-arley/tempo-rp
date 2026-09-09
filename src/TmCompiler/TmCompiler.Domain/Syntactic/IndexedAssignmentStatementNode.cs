namespace TmCompiler.Domain.Syntactic;

/// <summary>
/// Representa una asignación sobre una posición de sequence o score.
///
/// Ejemplos:
/// numeros[i] = 10;
/// matriz[i][j] = 20;
/// </summary>
public sealed record IndexedAssignmentStatementNode(
    IndexExpressionNode Target,
    ExpressionNode Value,
    int Line,
    int Column
) : StatementNode;