namespace TmCompiler.Domain.Syntactic;

/// <summary>
/// Representa una expresión utilizada como sentencia.
///
/// Ejemplo futuro:
/// saludar("Ana");
/// </summary>
public sealed record ExpressionStatementNode(
    ExpressionNode Expression,
    int Line,
    int Column
) : StatementNode;