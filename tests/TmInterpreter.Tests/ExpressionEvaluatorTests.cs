using TmCompiler.Domain.Lexical;
using TmCompiler.Domain.Syntactic;
using TmInterpreter.Application;
using TmInterpreter.Domain;
using Xunit;

namespace TmInterpreter.Tests;

public sealed class ExpressionEvaluatorTests
{
    private readonly ExpressionEvaluator _evaluator = new();

    [Fact]
    public void EvaluatesSampleLiteral()
    {
        var environment = new RuntimeEnvironment();

        RuntimeValue result = _evaluator.Evaluate(
            Sample(10),
            environment);

        Assert.Equal(RuntimeValueKind.Sample, result.Kind);
        Assert.Equal(10, result.Value);
    }

    [Fact]
    public void EvaluatesIdentifierFromEnvironment()
    {
        var environment = new RuntimeEnvironment();

        environment.Declare(
            "numero",
            RuntimeValue.Sample(25));

        var expression =
            new IdentifierExpressionNode(
                "numero",
                1,
                1);

        RuntimeValue result =
            _evaluator.Evaluate(
                expression,
                environment);

        Assert.Equal(25, result.Value);
    }

    [Fact]
    public void EvaluatesNestedArithmeticExpression()
    {
        var environment = new RuntimeEnvironment();

        var multiplication =
            new BinaryExpressionNode(
                Sample(5),
                TokenType.Multiply,
                Sample(2),
                1,
                1);

        var addition =
            new BinaryExpressionNode(
                Sample(10),
                TokenType.Plus,
                multiplication,
                1,
                1);

        RuntimeValue result =
            _evaluator.Evaluate(
                addition,
                environment);

        Assert.Equal(20, result.Value);
    }

    [Fact]
    public void SampleDivisionUsesIntegerDivision()
    {
        var environment = new RuntimeEnvironment();

        var expression =
            new BinaryExpressionNode(
                Sample(10),
                TokenType.Divide,
                Sample(3),
                1,
                1);

        RuntimeValue result =
            _evaluator.Evaluate(
                expression,
                environment);

        Assert.Equal(RuntimeValueKind.Sample, result.Kind);
        Assert.Equal(3, result.Value);
    }

    [Fact]
    public void EvaluatesWaveArithmetic()
    {
        var environment = new RuntimeEnvironment();

        var expression =
            new BinaryExpressionNode(
                Wave("3.5"),
                TokenType.Plus,
                Wave("2.5"),
                1,
                1);

        RuntimeValue result =
            _evaluator.Evaluate(
                expression,
                environment);

        Assert.Equal(RuntimeValueKind.Wave, result.Kind);
        Assert.Equal(6.0f, result.Value);
    }

    [Fact]
    public void ConcatenatesPhraseAndSample()
    {
        var environment = new RuntimeEnvironment();

        var expression =
            new BinaryExpressionNode(
                Phrase("Edad: "),
                TokenType.Plus,
                Sample(20),
                1,
                1);

        RuntimeValue result =
            _evaluator.Evaluate(
                expression,
                environment);

        Assert.Equal(
            RuntimeValueKind.Phrase,
            result.Kind);

        Assert.Equal(
            "Edad: 20",
            result.Value);
    }

    [Fact]
    public void EvaluatesRelationalExpression()
    {
        var environment = new RuntimeEnvironment();

        var expression =
            new BinaryExpressionNode(
                Sample(10),
                TokenType.GreaterThan,
                Sample(5),
                1,
                1);

        RuntimeValue result =
            _evaluator.Evaluate(
                expression,
                environment);

        Assert.Equal(true, result.Value);
    }

    [Fact]
    public void EvaluatesLogicalNot()
    {
        var environment = new RuntimeEnvironment();

        var expression =
            new UnaryExpressionNode(
                TokenType.Not,
                Signal(true),
                1,
                1);

        RuntimeValue result =
            _evaluator.Evaluate(
                expression,
                environment);

        Assert.Equal(false, result.Value);
    }

    [Fact]
    public void EvaluatesUnaryMinus()
    {
        var environment = new RuntimeEnvironment();

        var expression =
            new UnaryExpressionNode(
                TokenType.Minus,
                Sample(10),
                1,
                1);

        RuntimeValue result =
            _evaluator.Evaluate(
                expression,
                environment);

        Assert.Equal(-10, result.Value);
    }

    [Fact]
    public void AndUsesShortCircuitEvaluation()
    {
        var environment = new RuntimeEnvironment();

        var divisionByZero =
            new BinaryExpressionNode(
                Sample(10),
                TokenType.Divide,
                Sample(0),
                1,
                1);

        var comparison =
            new BinaryExpressionNode(
                divisionByZero,
                TokenType.GreaterThan,
                Sample(1),
                1,
                1);

        var expression =
            new BinaryExpressionNode(
                Signal(false),
                TokenType.And,
                comparison,
                1,
                1);

        RuntimeValue result =
            _evaluator.Evaluate(
                expression,
                environment);

        Assert.Equal(false, result.Value);
    }

    [Fact]
    public void DivisionByZeroProducesR001()
    {
        var environment = new RuntimeEnvironment();

        var expression =
            new BinaryExpressionNode(
                Sample(10),
                TokenType.Divide,
                Sample(0),
                7,
                1);

        TmRuntimeException exception =
            Assert.Throws<TmRuntimeException>(() =>
                _evaluator.Evaluate(
                    expression,
                    environment));

        Assert.Equal(
            RuntimeErrorCodes.DivisionByZero,
            exception.Code);

        Assert.Equal(7, exception.Line);
    }

    [Fact]
    public void ArithmeticOverflowProducesR003()
    {
        var environment = new RuntimeEnvironment();

        var expression =
            new BinaryExpressionNode(
                Sample(int.MaxValue),
                TokenType.Plus,
                Sample(1),
                4,
                1);

        TmRuntimeException exception =
            Assert.Throws<TmRuntimeException>(() =>
                _evaluator.Evaluate(
                    expression,
                    environment));

        Assert.Equal(
            RuntimeErrorCodes.ArithmeticOverflow,
            exception.Code);
    }

    [Fact]
    public void ComparesPhrasesByValue()
    {
        var environment = new RuntimeEnvironment();

        var expression =
            new BinaryExpressionNode(
                Phrase("tempo"),
                TokenType.Equal,
                Phrase("tempo"),
                1,
                1);

        RuntimeValue result =
            _evaluator.Evaluate(
                expression,
                environment);

        Assert.Equal(true, result.Value);
    }

    private static LiteralExpressionNode Sample(
        int value)
    {
        return new LiteralExpressionNode(
            TokenType.IntegerLiteral,
            value.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            1,
            1);
    }

    private static LiteralExpressionNode Wave(
        string value)
    {
        return new LiteralExpressionNode(
            TokenType.FloatLiteral,
            value,
            1,
            1);
    }

    private static LiteralExpressionNode Phrase(
        string value)
    {
        return new LiteralExpressionNode(
            TokenType.StringLiteral,
            value,
            1,
            1);
    }

    private static LiteralExpressionNode Signal(
        bool value)
    {
        return new LiteralExpressionNode(
            value
                ? TokenType.True
                : TokenType.False,
            value
                ? "true"
                : "false",
            1,
            1);
    }
}