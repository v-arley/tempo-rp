namespace Tempo.Language.Syntax;

/// <summary>
/// Representa el ciclo loop de TM, equivalente a un for.
///
/// Ejemplo:
/// loop [sample i = 0; i < 5; i++] >
///     output >> i;
/// <
/// </summary>
public sealed record LoopStatementNode(
    VariableDeclarationNode Initializer,
    ExpressionNode Condition,
    LoopUpdateNode Update,
    BlockNode Body,
    int Line,
    int Column
) : StatementNode;