namespace TmCompiler.Domain.Syntactic;

/// <summary>
/// Representa la sentencia condicional múltiple mix de TM.
///
/// Ejemplo:
/// mix opcion >
///     case 1: > ... <
///     case 2: > ... <
///     default: > ... <
/// <
/// </summary>
public sealed record MixStatementNode(
    ExpressionNode Expression,
    IReadOnlyList<MixCaseNode> Cases,
    BlockNode DefaultBlock,
    int Line,
    int Column
) : StatementNode;