using Tempo.Language.Syntax;

namespace Tempo.Language.Binding;

/// <summary>
/// Determina si un bloque garantiza el retorno por todas las trayectorias de
/// ejecución (§5.3). Es un análisis puramente estructural: no evalúa
/// condiciones, así que <c>loop</c> y <c>repeat</c> nunca garantizan retorno
/// aunque su condición sea constante.
/// </summary>

public static class ReturnPathAnalyzer
{
    public static bool AlwaysReturns(BlockNode block)
    {
        if (block is null)
        {
            return false;
        }

        foreach (StatementNode statement in block.Statements)
        {
            if (AlwaysReturns(statement))
            {
                return true;
            }
        }

        return false;
    }

    private static bool AlwaysReturns(StatementNode statement)
    {
        switch (statement)
        {
            case ReturnStatementNode:
                return true;

            case IfStatementNode ifStatement:
                return AlwaysReturnsIf(ifStatement);

            case MixStatementNode mix:
                return AlwaysReturnsMix(mix);

            default:
                return false;
        }
    }

    private static bool AlwaysReturnsIf(IfStatementNode ifStatement)
    {
        // Sin else hay una trayectoria que no retorna.
        if (ifStatement.ElseBlock is null)
        {
            return false;
        }

        if (!AlwaysReturns(ifStatement.ThenBlock) || !AlwaysReturns(ifStatement.ElseBlock))
        {
            return false;
        }

        foreach (ElseIfClauseNode clause in ifStatement.ElseIfClauses)
        {
            if (!AlwaysReturns(clause.Body))
            {
                return false;
            }
        }

        return true;
    }

    private static bool AlwaysReturnsMix(MixStatementNode mix)
    {
        // Sin default, ningún case podría coincidir.
        if (mix.DefaultBlock is null || !AlwaysReturns(mix.DefaultBlock))
        {
            return false;
        }

        foreach (MixCaseNode mixCase in mix.Cases)
        {
            if (!AlwaysReturns(mixCase.Body))
            {
                return false;
            }
        }

        return true;
    }
}
