using Tempo.Language.Syntax;

namespace Tempo.Language.Binding;

/// <summary>
/// Decide si una expresión es constante en tiempo de compilación, según la
/// §5.2: "literales u operaciones entre literales". No calcula el valor, solo
/// la constancia, que es lo que exige la validación de <c>const</c>.
/// </summary>

public static class ConstantExpressionEvaluator
{
    public static bool IsConstant(ExpressionNode expression)
    {
        switch (expression)
        {
            case LiteralExpressionNode:
                return true;

            case UnaryExpressionNode unary:
                return IsConstant(unary.Operand);

            case BinaryExpressionNode binary:
                return IsConstant(binary.Left) && IsConstant(binary.Right);

            case CollectionLiteralExpressionNode collection:
                return AreAllConstant(collection.Elements);

            // Identificadores, llamadas, indexaciones y .length() dependen del
            // estado en ejecución.
            default:
                return false;
        }
    }

    private static bool AreAllConstant(IReadOnlyList<ExpressionNode> expressions)
    {
        foreach (ExpressionNode expression in expressions)
        {
            if (!IsConstant(expression))
            {
                return false;
            }
        }

        return true;
    }
}
