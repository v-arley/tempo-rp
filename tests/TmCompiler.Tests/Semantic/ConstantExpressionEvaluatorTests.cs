using TmCompiler.Application.Analysis;
using TmCompiler.Domain.Syntactic;
using Xunit;

namespace TmCompiler.Tests.Semantic;

/// <summary>
/// Constancia en tiempo de compilación según la §5.2: "literales u operaciones
/// entre literales".
/// </summary>

public sealed class ConstantExpressionEvaluatorTests
{
    [Theory]
    [InlineData("10")]
    [InlineData("3.5")]
    [InlineData("\"texto\"")]
    [InlineData("true")]
    [InlineData("2 + 3 * 4")]
    [InlineData("-5")]
    [InlineData("not true")]
    [InlineData("[1, 2, 3]")]
    public void IsConstant_LiteralOrOperationBetweenLiterals_IsTrue(string expression)
    {
        Assert.True(ConstantExpressionEvaluator.IsConstant(InitializerOf(expression)));
    }

    [Theory]
    [InlineData("x")]
    [InlineData("x + 5")]
    [InlineData("5 + x")]
    [InlineData("calcular()")]
    [InlineData("datos[0]")]
    [InlineData("datos.length()")]
    [InlineData("[1, x]")]
    public void IsConstant_DependsOnRuntimeState_IsFalse(string expression)
    {
        Assert.False(ConstantExpressionEvaluator.IsConstant(InitializerOf(expression)));
    }

    [Fact]
    public void IsConstant_NullExpression_IsFalse()
    {
        Assert.False(ConstantExpressionEvaluator.IsConstant(null));
    }

    /// <summary>
    /// Extrae la expresión inicializadora de una declaración, para no construir
    /// el AST a mano.
    /// </summary>
    private static ExpressionNode InitializerOf(string expression)
    {
        ProgramNode program = SemanticTestHarness.Parse(
            $"session >\n    sample valor = {expression};\n<");

        var declaration = (VariableDeclarationNode)program.Session.Body.Statements[0];
        return declaration.Initializer;
    }
}
