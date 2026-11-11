using System.Globalization;
using Tempo.Language.Syntax;

namespace Tempo.Language.Evaluation;

/// <summary>
/// Evalúa expresiones del AST de TM.
/// </summary>
public sealed class ExpressionEvaluator : IExpressionEvaluator
{
    private readonly IFunctionInvoker? _functions;

    public ExpressionEvaluator()
    {
    }

    public ExpressionEvaluator(
        IFunctionInvoker functions)
    {
        ArgumentNullException.ThrowIfNull(functions);

        _functions = functions;
    }

    private RuntimeValue EvaluateCall(
        CallExpressionNode call,
        RuntimeEnvironment environment)
    {
        if (_functions is null)
        {
            throw new InvalidOperationException(
                "Este evaluador no tiene un invocador " +
                "de funciones configurado.");
        }

        return _functions.Invoke(
            call,
            environment);
    }

    private RuntimeValue EvaluateCollectionLiteral(
        CollectionLiteralExpressionNode collection,
        RuntimeEnvironment environment)
    {
        var values =
            new List<RuntimeValue>(
                collection.Elements.Count);

        foreach (ExpressionNode element
                in collection.Elements)
        {
            values.Add(
                Evaluate(
                    element,
                    environment));
        }

        bool containsSequence =
            values.Any(
                value =>
                    value.Kind ==
                    RuntimeValueKind.Sequence);

        if (!containsSequence)
        {
            return RuntimeValue.Sequence(
                new RuntimeSequence(values));
        }

        bool allSequences =
            values.All(
                value =>
                    value.Kind ==
                    RuntimeValueKind.Sequence);

        if (!allSequences)
        {
            throw new InvalidOperationException(
                "Un literal de score no puede mezclar filas " +
                "con valores escalares.");
        }

        var rows =
            new List<RuntimeSequence>(
                values.Count);

        foreach (RuntimeValue value in values)
        {
            rows.Add(
                (RuntimeSequence)value.Value!);
        }

        return RuntimeValue.Score(
            new RuntimeScore(rows));
    }

    private RuntimeValue EvaluateIndex(
        IndexExpressionNode indexExpression,
        RuntimeEnvironment environment)
    {
        RuntimeValue target =
            Evaluate(
                indexExpression.Target,
                environment);

        RuntimeValue indexValue =
            Evaluate(
                indexExpression.Index,
                environment);

        int index =
            RuntimeCollectionAccess.ReadIndex(
                indexValue);

        return RuntimeCollectionAccess.Read(
            target,
            index,
            indexExpression.Line);
    }

    private RuntimeValue EvaluateLength(
        LengthExpressionNode lengthExpression,
        RuntimeEnvironment environment)
    {
        RuntimeValue target =
            Evaluate(
                lengthExpression.Target,
                environment);

        return RuntimeCollectionAccess.Length(
            target);
    }

    public RuntimeValue Evaluate(
        ExpressionNode expression,
        RuntimeEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(environment);

        return expression switch
        {
            LiteralExpressionNode literal =>
                EvaluateLiteral(literal),

            IdentifierExpressionNode identifier =>
                EvaluateIdentifier(
                    identifier,
                    environment),

            UnaryExpressionNode unary =>
                EvaluateUnary(
                    unary,
                    environment),

            BinaryExpressionNode binary =>
                EvaluateBinary(
                    binary,
                    environment),

            CallExpressionNode call =>
                EvaluateCall(
                    call,
                    environment),

            CollectionLiteralExpressionNode collection =>
                EvaluateCollectionLiteral(
                    collection,
                    environment),

            IndexExpressionNode index =>
                EvaluateIndex(
                    index,
                    environment),

            LengthExpressionNode length =>
                EvaluateLength(
                    length,
                    environment),

            _ => throw new NotSupportedException(
                $"La expresión '{expression.GetType().Name}' " +
                "todavía no está soportada por el intérprete.")
        };
    }

    private static RuntimeValue EvaluateLiteral(
        LiteralExpressionNode literal)
    {
        return literal.LiteralType switch
        {
            TokenType.IntegerLiteral =>
                RuntimeValue.Sample(
                    int.Parse(
                        literal.Value,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture)),

            TokenType.FloatLiteral =>
                RuntimeValue.Wave(
                    float.Parse(
                        literal.Value,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture)),

            // El lexer entrega cadenas y caracteres sin comillas y con los escapes resueltos
            TokenType.StringLiteral =>
                RuntimeValue.Phrase(
                    literal.Value),

            TokenType.CharLiteral =>
                RuntimeValue.Tone(
                    literal.Value[0]),

            TokenType.True =>
                RuntimeValue.Signal(true),

            TokenType.False =>
                RuntimeValue.Signal(false),

            TokenType.BoolLiteral =>
                RuntimeValue.Signal(
                    bool.Parse(literal.Value)),

            TokenType.Silence =>
                RuntimeValue.Silence(),

            TokenType.NullLiteral =>
                RuntimeValue.Silence(),

            _ => throw new InvalidOperationException(
                $"Literal TM no soportado: {literal.LiteralType}.")
        };
    }

    private static RuntimeValue EvaluateIdentifier(
        IdentifierExpressionNode identifier,
        RuntimeEnvironment environment)
    {
        RuntimeVariable variable =
            environment.Resolve(identifier.Name);

        return variable.Value;
    }

    private RuntimeValue EvaluateUnary(
        UnaryExpressionNode unary,
        RuntimeEnvironment environment)
    {
        RuntimeValue operand =
            Evaluate(unary.Operand, environment);

        try
        {
            return unary.Operator switch
            {
                TokenType.Minus =>
                    Negate(operand),

                TokenType.Not =>
                    LogicalNot(operand),

                _ => throw new InvalidOperationException(
                    $"Operador unario no soportado: {unary.Operator}.")
            };
        }
        catch (OverflowException)
        {
            throw new TmRuntimeException(
                RuntimeErrorCodes.ArithmeticOverflow,
                unary.Line,
                "Se produjo un desbordamiento aritmético.");
        }
    }

    private RuntimeValue EvaluateBinary(
        BinaryExpressionNode binary,
        RuntimeEnvironment environment)
    {
        // Cortocircuito para AND.
        if (binary.Operator == TokenType.And)
        {
            RuntimeValue left =
                Evaluate(binary.Left, environment);

            EnsureSignal(left);

            if (!(bool)left.Value!)
            {
                return RuntimeValue.Signal(false);
            }

            RuntimeValue right =
                Evaluate(binary.Right, environment);

            EnsureSignal(right);

            return RuntimeValue.Signal((bool)right.Value!);
        }

        // Cortocircuito para OR.
        if (binary.Operator == TokenType.Or)
        {
            RuntimeValue left =
                Evaluate(binary.Left, environment);

            EnsureSignal(left);

            if ((bool)left.Value!)
            {
                return RuntimeValue.Signal(true);
            }

            RuntimeValue right =
                Evaluate(binary.Right, environment);

            EnsureSignal(right);

            return RuntimeValue.Signal((bool)right.Value!);
        }

        RuntimeValue leftValue =
            Evaluate(binary.Left, environment);

        RuntimeValue rightValue =
            Evaluate(binary.Right, environment);

        try
        {
            return binary.Operator switch
            {
                TokenType.Plus =>
                    Add(leftValue, rightValue),

                TokenType.Minus =>
                    Subtract(leftValue, rightValue),

                TokenType.Multiply =>
                    Multiply(leftValue, rightValue),

                TokenType.Divide =>
                    Divide(
                        leftValue,
                        rightValue,
                        binary.Line),

                TokenType.Equal =>
                    RuntimeValue.Signal(
                        AreEqual(leftValue, rightValue)),

                TokenType.NotEqual =>
                    RuntimeValue.Signal(
                        !AreEqual(leftValue, rightValue)),

                TokenType.GreaterThan =>
                    Compare(
                        leftValue,
                        rightValue,
                        comparison => comparison > 0),

                TokenType.LessThan =>
                    Compare(
                        leftValue,
                        rightValue,
                        comparison => comparison < 0),

                TokenType.GreaterOrEqual =>
                    Compare(
                        leftValue,
                        rightValue,
                        comparison => comparison >= 0),

                TokenType.LessOrEqual =>
                    Compare(
                        leftValue,
                        rightValue,
                        comparison => comparison <= 0),

                _ => throw new InvalidOperationException(
                    $"Operador binario no soportado: {binary.Operator}.")
            };
        }
        catch (OverflowException)
        {
            throw new TmRuntimeException(
                RuntimeErrorCodes.ArithmeticOverflow,
                binary.Line,
                "Se produjo un desbordamiento aritmético.");
        }
    }

    private static RuntimeValue Add(
        RuntimeValue left,
        RuntimeValue right)
    {
        if (left.Kind == RuntimeValueKind.Sample &&
            right.Kind == RuntimeValueKind.Sample)
        {
            int result = checked(
                (int)left.Value! +
                (int)right.Value!);

            return RuntimeValue.Sample(result);
        }

        if (left.Kind == RuntimeValueKind.Wave &&
            right.Kind == RuntimeValueKind.Wave)
        {
            return RuntimeValue.Wave(
                (float)left.Value! +
                (float)right.Value!);
        }

        if (left.Kind == RuntimeValueKind.Phrase &&
            IsPhraseConcatenable(right.Kind))
        {
            return RuntimeValue.Phrase(
                (string)left.Value! +
                ToTmText(right));
        }

        if (right.Kind == RuntimeValueKind.Phrase &&
            IsPhraseConcatenable(left.Kind))
        {
            return RuntimeValue.Phrase(
                ToTmText(left) +
                (string)right.Value!);
        }

        throw new InvalidOperationException(
            $"No se puede aplicar '+' entre " +
            $"'{left.Kind}' y '{right.Kind}'.");
    }

    private static RuntimeValue Subtract(
        RuntimeValue left,
        RuntimeValue right)
    {
        if (left.Kind == RuntimeValueKind.Sample &&
            right.Kind == RuntimeValueKind.Sample)
        {
            return RuntimeValue.Sample(
                checked(
                    (int)left.Value! -
                    (int)right.Value!));
        }

        if (left.Kind == RuntimeValueKind.Wave &&
            right.Kind == RuntimeValueKind.Wave)
        {
            return RuntimeValue.Wave(
                (float)left.Value! -
                (float)right.Value!);
        }

        throw new InvalidOperationException(
            $"No se puede aplicar '-' entre " +
            $"'{left.Kind}' y '{right.Kind}'.");
    }

    private static RuntimeValue Multiply(
        RuntimeValue left,
        RuntimeValue right)
    {
        if (left.Kind == RuntimeValueKind.Sample &&
            right.Kind == RuntimeValueKind.Sample)
        {
            return RuntimeValue.Sample(
                checked(
                    (int)left.Value! *
                    (int)right.Value!));
        }

        if (left.Kind == RuntimeValueKind.Wave &&
            right.Kind == RuntimeValueKind.Wave)
        {
            return RuntimeValue.Wave(
                (float)left.Value! *
                (float)right.Value!);
        }

        throw new InvalidOperationException(
            $"No se puede aplicar '*' entre " +
            $"'{left.Kind}' y '{right.Kind}'.");
    }

    private static RuntimeValue Divide(
        RuntimeValue left,
        RuntimeValue right,
        int line)
    {
        if (left.Kind == RuntimeValueKind.Sample &&
            right.Kind == RuntimeValueKind.Sample)
        {
            int divisor = (int)right.Value!;

            if (divisor == 0)
            {
                throw new TmRuntimeException(
                    RuntimeErrorCodes.DivisionByZero,
                    line,
                    "No se puede dividir entre cero.");
            }

            return RuntimeValue.Sample(
                checked(
                    (int)left.Value! / divisor));
        }

        if (left.Kind == RuntimeValueKind.Wave &&
            right.Kind == RuntimeValueKind.Wave)
        {
            float divisor = (float)right.Value!;

            if (divisor == 0.0f)
            {
                throw new TmRuntimeException(
                    RuntimeErrorCodes.DivisionByZero,
                    line,
                    "No se puede dividir entre cero.");
            }

            return RuntimeValue.Wave(
                (float)left.Value! / divisor);
        }

        throw new InvalidOperationException(
            $"No se puede aplicar '/' entre " +
            $"'{left.Kind}' y '{right.Kind}'.");
    }

    private static RuntimeValue Negate(
        RuntimeValue operand)
    {
        if (operand.Kind == RuntimeValueKind.Sample)
        {
            return RuntimeValue.Sample(
                checked(-(int)operand.Value!));
        }

        if (operand.Kind == RuntimeValueKind.Wave)
        {
            return RuntimeValue.Wave(
                -(float)operand.Value!);
        }

        throw new InvalidOperationException(
            $"No se puede aplicar '-' a '{operand.Kind}'.");
    }

    private static RuntimeValue LogicalNot(
        RuntimeValue operand)
    {
        EnsureSignal(operand);

        return RuntimeValue.Signal(
            !(bool)operand.Value!);
    }

    private static RuntimeValue Compare(
        RuntimeValue left,
        RuntimeValue right,
        Func<int, bool> condition)
    {
        if (left.Kind == RuntimeValueKind.Sample &&
            right.Kind == RuntimeValueKind.Sample)
        {
            int comparison =
                ((int)left.Value!).CompareTo(
                    (int)right.Value!);

            return RuntimeValue.Signal(
                condition(comparison));
        }

        if (left.Kind == RuntimeValueKind.Wave &&
            right.Kind == RuntimeValueKind.Wave)
        {
            int comparison =
                ((float)left.Value!).CompareTo(
                    (float)right.Value!);

            return RuntimeValue.Signal(
                condition(comparison));
        }

        throw new InvalidOperationException(
            $"No se pueden comparar relacionalmente " +
            $"'{left.Kind}' y '{right.Kind}'.");
    }

    private static bool AreEqual(
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
                $"No se puede comparar el tipo '{left.Kind}'.")
        };
    }

    private static void EnsureSignal(
        RuntimeValue value)
    {
        if (value.Kind != RuntimeValueKind.Signal)
        {
            throw new InvalidOperationException(
                "La operación lógica requiere valores signal.");
        }
    }

    private static bool IsPhraseConcatenable(
        RuntimeValueKind kind)
    {
        return kind == RuntimeValueKind.Sample ||
               kind == RuntimeValueKind.Wave ||
               kind == RuntimeValueKind.Tone ||
               kind == RuntimeValueKind.Phrase ||
               kind == RuntimeValueKind.Signal;
    }

    private static string ToTmText(
        RuntimeValue value)
    {
        return value.Kind switch
        {
            RuntimeValueKind.Sample =>
                ((int)value.Value!)
                .ToString(CultureInfo.InvariantCulture),

            RuntimeValueKind.Wave =>
                FormatWave((float)value.Value!),

            RuntimeValueKind.Tone =>
                ((char)value.Value!).ToString(),

            RuntimeValueKind.Phrase =>
                (string)value.Value!,

            RuntimeValueKind.Signal =>
                (bool)value.Value! ? "true" : "false",

            RuntimeValueKind.Silence =>
                "silence",

            _ => throw new InvalidOperationException(
                $"El tipo '{value.Kind}' no posee representación textual aquí.")
        };
    }

    private static string FormatWave(float value)
    {
        string text =
            value.ToString(
                "G9",
                CultureInfo.InvariantCulture);

        if (!text.Contains('.', StringComparison.Ordinal) &&
            !text.Contains('E', StringComparison.OrdinalIgnoreCase))
        {
            text += ".0";
        }

        return text;
    }

}