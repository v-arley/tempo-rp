using TmCompiler.Domain.Syntactic;
using TmInterpreter.Domain;

namespace TmInterpreter.Application;

/// <summary>
/// Permite evaluar llamadas a funciones TM.
/// </summary>
public interface IFunctionInvoker
{
    RuntimeValue Invoke(
    CallExpressionNode callExpression,
    RuntimeEnvironment callerEnvironment);
}