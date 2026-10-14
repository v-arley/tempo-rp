using Tempo.Language.Syntax;

namespace Tempo.Language.Evaluation;

/// <summary>
/// Evalúa expresiones del AST de TM y produce valores de runtime.
/// </summary>
public interface IExpressionEvaluator
{
    RuntimeValue Evaluate(
        ExpressionNode expression,
        RuntimeEnvironment environment);
}