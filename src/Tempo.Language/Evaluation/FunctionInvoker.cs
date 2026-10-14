using Tempo.Language.Syntax;

namespace Tempo.Language.Evaluation;

/// <summary>
/// Ejecuta llamadas a funciones TM.
/// Cada llamada crea un nuevo marco de activación.
/// </summary>
public sealed class FunctionInvoker : IFunctionInvoker
{
    private readonly FunctionRegistry _registry;
    private readonly ExpressionEvaluator _expressions;

    public FunctionInvoker(
        FunctionRegistry registry,
        IRuntimeInput input,
        IRuntimeOutput output)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        _registry = registry;

        CallStack = new RuntimeCallTracker();

        _expressions =
            new ExpressionEvaluator(this);

        Statements =
            new StatementExecutor(
                _expressions,
                input,
                output);
    }

    public RuntimeCallTracker CallStack { get; }

    public StatementExecutor Statements { get; }

    public RuntimeValue Invoke(
        CallExpressionNode callExpression,
        RuntimeEnvironment callerEnvironment)
    {
        ArgumentNullException.ThrowIfNull(callExpression);
        ArgumentNullException.ThrowIfNull(callerEnvironment);

        if (!_registry.TryResolve(
                callExpression.Identifier,
                out FunctionDeclarationNode? function))
        {
            throw new TmRuntimeException(
                RuntimeErrorCodes.UndefinedFunction,
                callExpression.Line,
                $"La función '{callExpression.Identifier}' no está definida.");
        }

        if (callExpression.Arguments.Count != function.Parameters.Count)
        {
            throw new InvalidOperationException(
                $"La función '{function.Identifier}' esperaba " +
                $"{function.Parameters.Count} argumentos, " +
                $"pero recibió {callExpression.Arguments.Count}.");
        }

        RuntimeValue[] arguments =
            EvaluateArguments(
                callExpression,
                callerEnvironment);

        var functionEnvironment =
            new RuntimeEnvironment();

        BindParameters(
            function,
            arguments,
            functionEnvironment);

        var frame =
            new RuntimeCallFrame(
                function.Identifier,
                functionEnvironment);

        CallStack.Push(frame);

        try
        {
            try
            {
                Statements.ExecuteBlock(
                    function.Body,
                    functionEnvironment);
            }
            catch (ReturnSignal signal)
            {
                return signal.Value;
            }

            if (string.Equals(
                    function.ReturnType,
                    "void",
                    StringComparison.Ordinal))
            {
                return RuntimeValue.Void();
            }

            throw new InvalidOperationException(
                $"La función '{function.Identifier}' " +
                "terminó sin ejecutar una sentencia ret.");
        }
        finally
        {
            CallStack.Pop();
        }
    }

    private RuntimeValue[] EvaluateArguments(
        CallExpressionNode call,
        RuntimeEnvironment callerEnvironment)
    {
        var arguments =
            new RuntimeValue[call.Arguments.Count];

        for (int index = 0;
             index < call.Arguments.Count;
             index++)
        {
            arguments[index] =
                _expressions.Evaluate(
                    call.Arguments[index],
                    callerEnvironment);
        }

        return arguments;
    }

    private static void BindParameters(
        FunctionDeclarationNode function,
        RuntimeValue[] arguments,
        RuntimeEnvironment environment)
    {
        for (int index = 0;
             index < function.Parameters.Count;
             index++)
        {
            ParameterNode parameter =
                function.Parameters[index];

            RuntimeValueKind declaredKind =
                RuntimeTypeResolver.Resolve(
                    parameter.TypeName);

            environment.Declare(
                parameter.Identifier,
                declaredKind,
                arguments[index]);
        }
    }
}