using System.Globalization;
using Tempo.Language.Syntax;

namespace Tempo.Language.Evaluation;

/// <summary>
/// Ejecuta las sentencias de un programa TM.
/// </summary>
public sealed class StatementExecutor
{
    private readonly IExpressionEvaluator _expressions;
    private readonly IRuntimeInput _input;
    private readonly IRuntimeOutput _output;

    public StatementExecutor(
        IExpressionEvaluator expressions,
        IRuntimeInput input,
        IRuntimeOutput output)
    {
        ArgumentNullException.ThrowIfNull(expressions);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        _expressions = expressions;
        _input = input;
        _output = output;
    }

    public void ExecuteBlock(
        BlockNode block,
        RuntimeEnvironment parentEnvironment)
    {
        ArgumentNullException.ThrowIfNull(block);
        ArgumentNullException.ThrowIfNull(parentEnvironment);

        var environment =
            new RuntimeEnvironment(parentEnvironment);

        foreach (StatementNode statement in block.Statements)
        {
            ExecuteStatement(
                statement,
                environment);
        }
    }

    private void ExecuteReturn(
        ReturnStatementNode statement,
        RuntimeEnvironment environment)
    {
        RuntimeValue value =
            statement.Expression is null
                ? RuntimeValue.Void()
                : _expressions.Evaluate(
                    statement.Expression,
                    environment);

        throw new ReturnSignal(value);
    }

    private void ExecuteExpressionStatement(
        ExpressionStatementNode statement,
        RuntimeEnvironment environment)
    {
        _expressions.Evaluate(
            statement.Expression,
            environment);
    }

    public void ExecuteStatement(
        StatementNode statement,
        RuntimeEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(statement);
        ArgumentNullException.ThrowIfNull(environment);

        switch (statement)
        {
            case VariableDeclarationNode variable:
                ExecuteVariableDeclaration(
                    variable,
                    environment);
                break;

            case ConstantDeclarationNode constant:
                ExecuteConstantDeclaration(
                    constant,
                    environment);
                break;

            case AssignmentStatementNode assignment:
                ExecuteAssignment(
                    assignment,
                    environment);
                break;

            case UpdateStatementNode update:
                ExecuteUpdate(
                    update,
                    environment);
                break;

            case OutputStatementNode output:
                ExecuteOutput(
                    output,
                    environment);
                break;

            case InputStatementNode input:
                ExecuteInput(
                    input,
                    environment);
                break;

            case IfStatementNode ifStatement:
                ExecuteIf(
                    ifStatement,
                    environment);
                break;

            case RepeatStatementNode repeat:
                ExecuteRepeat(
                    repeat,
                    environment);
                break;

            case LoopStatementNode loop:
                ExecuteLoop(
                    loop,
                    environment);
                break;

            case MixStatementNode mix:
                ExecuteMix(
                    mix,
                    environment);
                break;

            case ReturnStatementNode returnStatement:
                ExecuteReturn(
                    returnStatement,
                    environment);
                break;

            case ExpressionStatementNode expressionStatement:
                ExecuteExpressionStatement(
                    expressionStatement,
                    environment);
                break;

            case IndexedAssignmentStatementNode indexedAssignment:
                ExecuteIndexedAssignment(
                    indexedAssignment,
                    environment);
                break;    

            default:
                throw new NotSupportedException(
                    $"La sentencia '{statement.GetType().Name}' " +
                    "todavía no está soportada por el intérprete.");
        }
    }

    private void ExecuteVariableDeclaration(
        VariableDeclarationNode declaration,
        RuntimeEnvironment environment)
    {
        RuntimeValue value =
            _expressions.Evaluate(
                declaration.Initializer,
                environment);

        RuntimeValueKind declaredKind =
            RuntimeTypeResolver.Resolve(
                declaration.TypeName);

        environment.Declare(
            declaration.Identifier,
            declaredKind,
            value);
    }

    private void ExecuteConstantDeclaration(
        ConstantDeclarationNode declaration,
        RuntimeEnvironment environment)
    {
        RuntimeValue value =
            _expressions.Evaluate(
                declaration.Initializer,
                environment);

        RuntimeValueKind declaredKind =
            RuntimeTypeResolver.Resolve(
                declaration.TypeName);

        environment.Declare(
            declaration.Identifier,
            declaredKind,
            value,
            true);
    }

    private void ExecuteAssignment(
        AssignmentStatementNode assignment,
        RuntimeEnvironment environment)
    {
        RuntimeValue value =
            _expressions.Evaluate(
                assignment.Value,
                environment);

        environment.Assign(
            assignment.Identifier,
            value);
    }

    private static void ExecuteUpdate(
        UpdateStatementNode update,
        RuntimeEnvironment environment)
    {
        ApplySampleUpdate(
            update.Identifier,
            update.Operator,
            update.Line,
            environment);
    }

    private void ExecuteOutput(
        OutputStatementNode output,
        RuntimeEnvironment environment)
    {
        RuntimeValue value =
            _expressions.Evaluate(
                output.Expression,
                environment);

        _output.Write(
            RuntimeValueFormatter.ToText(value));
    }

    private void ExecuteInput(
        InputStatementNode input,
        RuntimeEnvironment environment)
    {
        RuntimeVariable variable =
            environment.Resolve(
                input.Identifier);

        string? line = _input.ReadLine();

        if (line is null)
        {
            throw new TmRuntimeException(
                RuntimeErrorCodes.InputEndOfFile,
                input.Line,
                "No hay más datos disponibles en la entrada.");
        }

        RuntimeValue value =
            ParseInput(
                line,
                variable.DeclaredKind,
                input.Line);

        variable.Assign(value);
    }

    private void ExecuteIf(
        IfStatementNode statement,
        RuntimeEnvironment environment)
    {
        if (EvaluateCondition(
                statement.Condition,
                environment))
        {
            ExecuteBlock(
                statement.ThenBlock,
                environment);

            return;
        }

        foreach (ElseIfClauseNode clause
                 in statement.ElseIfClauses)
        {
            if (!EvaluateCondition(
                    clause.Condition,
                    environment))
            {
                continue;
            }

            ExecuteBlock(
                clause.Body,
                environment);

            return;
        }

        if (statement.ElseBlock is not null)
        {
            ExecuteBlock(
                statement.ElseBlock,
                environment);
        }
    }

    private void ExecuteRepeat(
        RepeatStatementNode statement,
        RuntimeEnvironment environment)
    {
        while (EvaluateCondition(
                   statement.Condition,
                   environment))
        {
            ExecuteBlock(
                statement.Body,
                environment);
        }
    }

    private void ExecuteLoop(
        LoopStatementNode statement,
        RuntimeEnvironment environment)
    {
        // El inicializador pertenece al ámbito del loop,
        // no al ámbito que contiene al loop.
        var loopEnvironment =
            new RuntimeEnvironment(environment);

        ExecuteVariableDeclaration(
            statement.Initializer,
            loopEnvironment);

        while (EvaluateCondition(
                   statement.Condition,
                   loopEnvironment))
        {
            ExecuteBlock(
                statement.Body,
                loopEnvironment);

            ExecuteLoopUpdate(
                statement.Update,
                loopEnvironment);
        }
    }

    private void ExecuteLoopUpdate(
        LoopUpdateNode update,
        RuntimeEnvironment environment)
    {
        if (update.Operator == TokenType.Increment ||
            update.Operator == TokenType.Decrement)
        {
            ApplySampleUpdate(
                update.Identifier,
                update.Operator,
                update.Line,
                environment);

            return;
        }

        if (update.Operator == TokenType.Assign)
        {
            if (update.Value is null)
            {
                throw new InvalidOperationException(
                    "La actualización por asignación del loop " +
                    "requiere una expresión.");
            }

            RuntimeValue value =
                _expressions.Evaluate(
                    update.Value,
                    environment);

            environment.Assign(
                update.Identifier,
                value);

            return;
        }

        throw new InvalidOperationException(
            $"Operador de actualización de loop no válido: " +
            $"'{update.Operator}'.");
    }

    private void ExecuteMix(
        MixStatementNode statement,
        RuntimeEnvironment environment)
    {
        RuntimeValue control =
            _expressions.Evaluate(
                statement.Expression,
                environment);

        foreach (MixCaseNode mixCase in statement.Cases)
        {
            RuntimeValue caseValue =
                _expressions.Evaluate(
                    mixCase.Value,
                    environment);

            if (!RuntimeValuesEqual(
                    control,
                    caseValue))
            {
                continue;
            }

            ExecuteBlock(
                mixCase.Body,
                environment);

            // TM no permite fall-through.
            return;
        }

        if (statement.DefaultBlock is not null)
        {
            ExecuteBlock(
                statement.DefaultBlock,
                environment);
        }
    }

    private void ExecuteIndexedAssignment(
        IndexedAssignmentStatementNode statement,
        RuntimeEnvironment environment)
    {
        RuntimeValue target =
            _expressions.Evaluate(
                statement.Target.Target,
                environment);

        RuntimeValue indexValue =
            _expressions.Evaluate(
                statement.Target.Index,
                environment);

        RuntimeValue value =
            _expressions.Evaluate(
                statement.Value,
                environment);

        int index =
            RuntimeCollectionAccess.ReadIndex(
                indexValue);

        RuntimeCollectionAccess.Write(
            target,
            index,
            value,
            statement.Target.Line);
    }

    private bool EvaluateCondition(
        ExpressionNode condition,
        RuntimeEnvironment environment)
    {
        RuntimeValue value =
            _expressions.Evaluate(
                condition,
                environment);

        if (value.Kind != RuntimeValueKind.Signal)
        {
            throw new InvalidOperationException(
                "La condición de control debe producir un valor signal.");
        }

        return (bool)value.Value!;
    }

    private static void ApplySampleUpdate(
        string identifier,
        TokenType operatorType,
        int line,
        RuntimeEnvironment environment)
    {
        RuntimeVariable variable =
            environment.Resolve(identifier);

        if (variable.IsConstant)
        {
            throw new InvalidOperationException(
                $"La constante '{identifier}' no puede modificarse.");
        }

        if (variable.Value.Kind != RuntimeValueKind.Sample)
        {
            throw new InvalidOperationException(
                $"La actualización '{operatorType}' " +
                "solo puede aplicarse a valores sample.");
        }

        int currentValue =
            (int)variable.Value.Value!;

        try
        {
            int newValue =
                operatorType switch
                {
                    TokenType.Increment =>
                        checked(currentValue + 1),

                    TokenType.Decrement =>
                        checked(currentValue - 1),

                    _ => throw new InvalidOperationException(
                        $"Operador de actualización no válido: " +
                        $"'{operatorType}'.")
                };

            variable.Assign(
                RuntimeValue.Sample(newValue));
        }
        catch (OverflowException)
        {
            throw new TmRuntimeException(
                RuntimeErrorCodes.ArithmeticOverflow,
                line,
                "Se produjo un desbordamiento aritmético.");
        }
    }

    private static bool RuntimeValuesEqual(
        RuntimeValue left,
        RuntimeValue right)
    {
        if (left.Kind != right.Kind)
        {
            return false;
        }

        return left.Kind switch
        {
            RuntimeValueKind.Sample =>
                (int)left.Value! ==
                (int)right.Value!,

            RuntimeValueKind.Wave =>
                (float)left.Value! ==
                (float)right.Value!,

            RuntimeValueKind.Tone =>
                (char)left.Value! ==
                (char)right.Value!,

            RuntimeValueKind.Phrase =>
                string.Equals(
                    (string)left.Value!,
                    (string)right.Value!,
                    StringComparison.Ordinal),

            RuntimeValueKind.Signal =>
                (bool)left.Value! ==
                (bool)right.Value!,

            RuntimeValueKind.Silence =>
                true,

            _ => throw new InvalidOperationException(
                $"El tipo '{left.Kind}' no puede utilizarse en mix.")
        };
    }

    private static RuntimeValue ParseInput(
        string line,
        RuntimeValueKind kind,
        int sourceLine)
    {
        switch (kind)
        {
            case RuntimeValueKind.Sample:
            {
                if (int.TryParse(
                        line.Trim(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int value))
                {
                    return RuntimeValue.Sample(value);
                }

                throw CreateInputParseException(
                    sourceLine,
                    "sample");
            }

            case RuntimeValueKind.Wave:
            {
                if (float.TryParse(
                        line.Trim(),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out float value))
                {
                    return RuntimeValue.Wave(value);
                }

                throw CreateInputParseException(
                    sourceLine,
                    "wave");
            }

            case RuntimeValueKind.Phrase:
                return RuntimeValue.Phrase(line);

            case RuntimeValueKind.Signal:
            {
                string text = line.Trim();

                if (string.Equals(
                        text,
                        "true",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return RuntimeValue.Signal(true);
                }

                if (string.Equals(
                        text,
                        "false",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return RuntimeValue.Signal(false);
                }

                throw CreateInputParseException(
                    sourceLine,
                    "signal");
            }

            case RuntimeValueKind.Tone:
            {
                foreach (char character in line)
                {
                    if (!char.IsWhiteSpace(character))
                    {
                        return RuntimeValue.Tone(character);
                    }
                }

                throw new TmRuntimeException(
                    RuntimeErrorCodes.InvalidToneInput,
                    sourceLine,
                    "La entrada para tone está vacía o " +
                    "no contiene un carácter válido.");
            }

            default:
                throw new InvalidOperationException(
                    $"El tipo '{kind}' no admite input.");
        }
    }

    private static TmRuntimeException CreateInputParseException(
        int line,
        string typeName)
    {
        return new TmRuntimeException(
            RuntimeErrorCodes.InputParseError,
            line,
            $"No se pudo interpretar la entrada como '{typeName}'.");
    }
}