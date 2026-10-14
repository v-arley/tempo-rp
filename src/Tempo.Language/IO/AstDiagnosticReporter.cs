using System.Text;
using Tempo.Language.Syntax;

namespace Tempo.Language.IO;

/// <summary>
/// Responsable de formatear y emitir el árbol de sintaxis abstracta.
/// Aplica SRP: separa la construcción del AST de su presentación.
/// </summary>

public static class AstDiagnosticReporter
{
    private const string IndentUnit = "  ";

    /// <summary>
    /// Genera un volcado indentado del AST para fines de depuración.
    /// </summary>
    public static string GenerateAstReport(ProgramNode program, string fileName)
    {
        ArgumentNullException.ThrowIfNull(program);

        var report = new StringBuilder();
        report.AppendLine($"=== Reporte Sintáctico: {fileName} ===");
        report.AppendLine(new string('-', 65));

        report.AppendLine($"Programa ({program.Imports.Count} importación(es), " +
            $"{program.Functions.Count} función(es), " +
            $"session: {(program.Session is null ? "no" : "sí")})");

        foreach (ImportNode import in program.Imports)
        {
            report.AppendLine($"{IndentUnit}Import '{import.FilePath}' (L{import.Line})");
        }

        foreach (FunctionDeclarationNode function in program.Functions)
        {
            AppendFunction(report, function, 1);
        }

        if (program.Session is not null)
        {
            report.AppendLine($"{IndentUnit}Session");
            AppendBlock(report, program.Session.Body, 2);
        }

        return report.ToString();
    }

    private static void AppendFunction(
        StringBuilder report,
        FunctionDeclarationNode function,
        int depth)
    {
        string parameters = string.Join(
            ", ",
            function.Parameters.Select(parameter => $"{parameter.TypeName} {parameter.Identifier}"));

        AppendLine(
            report,
            depth,
            $"Function {function.ReturnType} {function.Identifier}({parameters}) (L{function.Line})");

        AppendBlock(report, function.Body, depth + 1);
    }

    private static void AppendBlock(StringBuilder report, BlockNode block, int depth)
    {
        if (block is null)
        {
            return;
        }

        foreach (StatementNode statement in block.Statements)
        {
            AppendStatement(report, statement, depth);
        }
    }

    private static void AppendStatement(StringBuilder report, StatementNode statement, int depth)
    {
        switch (statement)
        {
            case VariableDeclarationNode declaration:
                AppendLine(
                    report,
                    depth,
                    $"VarDecl {declaration.TypeName} {declaration.Identifier} (L{declaration.Line})");
                AppendExpression(report, declaration.Initializer, depth + 1);
                break;

            case ConstantDeclarationNode constant:
                AppendLine(
                    report,
                    depth,
                    $"ConstDecl {constant.TypeName} {constant.Identifier} (L{constant.Line})");
                AppendExpression(report, constant.Initializer, depth + 1);
                break;

            case AssignmentStatementNode assignment:
                AppendLine(report, depth, $"Assign {assignment.Identifier} (L{assignment.Line})");
                AppendExpression(report, assignment.Value, depth + 1);
                break;

            case IndexedAssignmentStatementNode indexed:
                AppendLine(report, depth, $"IndexedAssign (L{indexed.Line})");
                AppendExpression(report, indexed.Target, depth + 1);
                AppendExpression(report, indexed.Value, depth + 1);
                break;

            case UpdateStatementNode update:
                AppendLine(
                    report,
                    depth,
                    $"Update {update.Identifier} {update.Operator} (L{update.Line})");
                break;

            case InputStatementNode input:
                AppendLine(report, depth, $"Input << {input.Identifier} (L{input.Line})");
                break;

            case OutputStatementNode output:
                AppendLine(report, depth, $"Output >> (L{output.Line})");
                AppendExpression(report, output.Expression, depth + 1);
                break;

            case ReturnStatementNode returnStatement:
                AppendLine(report, depth, $"Return (L{returnStatement.Line})");
                AppendExpression(report, returnStatement.Expression, depth + 1);
                break;

            case ExpressionStatementNode expressionStatement:
                AppendLine(report, depth, $"ExpressionStatement (L{expressionStatement.Line})");
                AppendExpression(report, expressionStatement.Expression, depth + 1);
                break;

            case IfStatementNode ifStatement:
                AppendIf(report, ifStatement, depth);
                break;

            case LoopStatementNode loop:
                AppendLoop(report, loop, depth);
                break;

            case RepeatStatementNode repeat:
                AppendLine(report, depth, $"Repeat (L{repeat.Line})");
                AppendLine(report, depth + 1, "Condition");
                AppendExpression(report, repeat.Condition, depth + 2);
                AppendLine(report, depth + 1, "Body");
                AppendBlock(report, repeat.Body, depth + 2);
                break;

            case MixStatementNode mix:
                AppendMix(report, mix, depth);
                break;

            default:
                AppendLine(report, depth, statement.GetType().Name);
                break;
        }
    }

    private static void AppendIf(StringBuilder report, IfStatementNode ifStatement, int depth)
    {
        AppendLine(report, depth, $"If (L{ifStatement.Line})");
        AppendLine(report, depth + 1, "Condition");
        AppendExpression(report, ifStatement.Condition, depth + 2);
        AppendLine(report, depth + 1, "Then");
        AppendBlock(report, ifStatement.ThenBlock, depth + 2);

        foreach (ElseIfClauseNode clause in ifStatement.ElseIfClauses)
        {
            AppendLine(report, depth + 1, "ElseIf");
            AppendExpression(report, clause.Condition, depth + 2);
            AppendBlock(report, clause.Body, depth + 2);
        }

        if (ifStatement.ElseBlock is not null)
        {
            AppendLine(report, depth + 1, "Else");
            AppendBlock(report, ifStatement.ElseBlock, depth + 2);
        }
    }

    private static void AppendLoop(StringBuilder report, LoopStatementNode loop, int depth)
    {
        AppendLine(report, depth, $"Loop (L{loop.Line})");
        AppendLine(report, depth + 1, "Initializer");
        AppendStatement(report, loop.Initializer, depth + 2);
        AppendLine(report, depth + 1, "Condition");
        AppendExpression(report, loop.Condition, depth + 2);

        if (loop.Update is not null)
        {
            AppendLine(
                report,
                depth + 1,
                $"Update {loop.Update.Identifier} {loop.Update.Operator}");
            AppendExpression(report, loop.Update.Value, depth + 2);
        }

        AppendLine(report, depth + 1, "Body");
        AppendBlock(report, loop.Body, depth + 2);
    }

    private static void AppendMix(StringBuilder report, MixStatementNode mix, int depth)
    {
        AppendLine(report, depth, $"Mix (L{mix.Line})");
        AppendLine(report, depth + 1, "Selector");
        AppendExpression(report, mix.Expression, depth + 2);

        foreach (MixCaseNode mixCase in mix.Cases)
        {
            AppendLine(report, depth + 1, "Case");
            AppendExpression(report, mixCase.Value, depth + 2);
            AppendBlock(report, mixCase.Body, depth + 2);
        }

        if (mix.DefaultBlock is not null)
        {
            AppendLine(report, depth + 1, "Default");
            AppendBlock(report, mix.DefaultBlock, depth + 2);
        }
    }

    private static void AppendExpression(StringBuilder report, ExpressionNode expression, int depth)
    {
        if (expression is null)
        {
            return;
        }

        switch (expression)
        {
            case LiteralExpressionNode literal:
                AppendLine(report, depth, $"Literal [{literal.LiteralType}] '{literal.Value}'");
                break;

            case IdentifierExpressionNode identifier:
                AppendLine(report, depth, $"Identifier '{identifier.Name}'");
                break;

            case BinaryExpressionNode binary:
                AppendLine(report, depth, $"Binary {binary.Operator}");
                AppendExpression(report, binary.Left, depth + 1);
                AppendExpression(report, binary.Right, depth + 1);
                break;

            case UnaryExpressionNode unary:
                AppendLine(report, depth, $"Unary {unary.Operator}");
                AppendExpression(report, unary.Operand, depth + 1);
                break;

            case CallExpressionNode call:
                AppendLine(
                    report,
                    depth,
                    $"Call '{call.Identifier}' ({call.Arguments.Count} argumento(s))");
                foreach (ExpressionNode argument in call.Arguments)
                {
                    AppendExpression(report, argument, depth + 1);
                }

                break;

            case IndexExpressionNode index:
                AppendLine(report, depth, "Index");
                AppendExpression(report, index.Target, depth + 1);
                AppendExpression(report, index.Index, depth + 1);
                break;

            case LengthExpressionNode length:
                AppendLine(report, depth, "Length");
                AppendExpression(report, length.Target, depth + 1);
                break;

            case CollectionLiteralExpressionNode collection:
                AppendLine(
                    report,
                    depth,
                    $"CollectionLiteral ({collection.Elements.Count} elemento(s))");
                foreach (ExpressionNode element in collection.Elements)
                {
                    AppendExpression(report, element, depth + 1);
                }

                break;

            default:
                AppendLine(report, depth, expression.GetType().Name);
                break;
        }
    }

    private static void AppendLine(StringBuilder report, int depth, string text)
    {
        report.Append(string.Concat(Enumerable.Repeat(IndentUnit, depth)));
        report.AppendLine(text);
    }
}
