namespace Tempo.Language.Syntax;

/// <summary>
/// Representa una sentencia de retorno TM.
///
/// Ejemplos:
/// ret;
/// ret resultado;
/// ret a + b;
/// </summary>
public sealed record ReturnStatementNode(
    ExpressionNode Expression,
    int Line,
    int Column
) : StatementNode;