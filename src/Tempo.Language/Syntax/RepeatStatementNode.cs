namespace Tempo.Language.Syntax;

/// <summary>
/// Representa el ciclo repeat de TM, equivalente a un while.
///
/// Ejemplo:
/// repeat [x < 10] >
///     x = x + 1;
/// <
/// </summary>
public sealed record RepeatStatementNode(
    ExpressionNode Condition,
    BlockNode Body,
    int Line,
    int Column
) : StatementNode;