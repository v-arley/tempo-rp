using TmCompiler.Domain.Syntactic;
using TmInterpreter.Domain;

namespace TmInterpreter.Application;

/// <summary>
/// Evalúa expresiones del AST de TM y produce valores de runtime.
/// </summary>
public interface IExpressionEvaluator
{
    RuntimeValue Evaluate(
        ExpressionNode expression,
        RuntimeEnvironment environment);
}