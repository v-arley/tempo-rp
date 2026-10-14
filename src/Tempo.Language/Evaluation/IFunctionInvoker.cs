using Tempo.Language.Syntax;

namespace Tempo.Language.Evaluation;

/// <summary>
/// Permite evaluar llamadas a funciones TM.
/// </summary>
public interface IFunctionInvoker
{
    RuntimeValue Invoke(
    CallExpressionNode callExpression,
    RuntimeEnvironment callerEnvironment);
}