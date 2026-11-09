using Tempo.Language.Syntax;

namespace Tempo.Language.Outline;

/// <summary>
/// Recorre un bloque y devuelve, en orden, todos los nombres que declara: las
/// variables y constantes de cualquier nivel de anidamiento y las variables de
/// inicialización de <c>loop</c>. Cada sentencia que contiene bloques indica
/// cuáles con una sola fila de la tabla de <see cref="In(StatementNode)"/>.
/// </summary>

internal static class LocalDeclarations
{
    public static IEnumerable<LocalOutline> In(BlockNode block) =>
        block?.Statements?.SelectMany(In) ?? Enumerable.Empty<LocalOutline>();

    private static IEnumerable<LocalOutline> In(StatementNode statement) => statement switch
    {
        VariableDeclarationNode variable => [new LocalOutline(variable.Identifier, variable.TypeName, LocalKind.Variable, variable.Line)],
        ConstantDeclarationNode constant => [new LocalOutline(constant.Identifier, constant.TypeName, LocalKind.Constant, constant.Line)],
        LoopStatementNode loop => In(loop.Initializer).Concat(In(loop.Body)),
        RepeatStatementNode repeat => In(repeat.Body),
        IfStatementNode conditional => In(conditional.ThenBlock)
            .Concat((conditional.ElseIfClauses ?? Array.Empty<ElseIfClauseNode>()).SelectMany(clause => In(clause.Body)))
            .Concat(In(conditional.ElseBlock)),
        MixStatementNode mix => (mix.Cases ?? Array.Empty<MixCaseNode>()).SelectMany(@case => In(@case.Body))
            .Concat(In(mix.DefaultBlock)),
        _ => Enumerable.Empty<LocalOutline>()
    };
}
