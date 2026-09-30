using System.Globalization;
using TmCompiler.Domain.Lexical;
using TmCompiler.Domain.Syntactic;
using TmInterpreter.Domain;

namespace TmInterpreter.Application;

/// <summary>
/// Ejecuta las sentencias de un programa TM.
/// En este bloque soporta declaraciones, asignaciones,
/// actualizaciones y entrada/salida.
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
        RuntimeVariable variable =
            environment.Resolve(
                update.Identifier);

        if (variable.IsConstant)
        {
            throw new InvalidOperationException(
                $"La constante '{update.Identifier}' no puede modificarse.");
        }

        int currentValue =
            (int)variable.Value.Value!;

        try
        {
            int newValue =
                update.Operator switch
                {
                    TokenType.Increment =>
                        checked(currentValue + 1),

                    TokenType.Decrement =>
                        checked(currentValue - 1),

                    _ => throw new InvalidOperationException(
                        $"Operador de actualización no válido: " +
                        $"'{update.Operator}'.")
                };

            variable.Assign(
                RuntimeValue.Sample(newValue));
        }
        catch (OverflowException)
        {
            throw new TmRuntimeException(
                RuntimeErrorCodes.ArithmeticOverflow,
                update.Line,
                "Se produjo un desbordamiento aritmético.");
        }
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
                    "La entrada para tone está vacía o no contiene un carácter válido.");
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