using Tempo.Language.Evaluation;
using Tempo.Language.Syntax;
using Xunit;

namespace Tempo.Language.Tests.Evaluation;

public sealed class StatementExecutorTests
{
    [Fact]
    public void VariableDeclarationStoresValue()
    {
        var environment = new RuntimeEnvironment();
        var input = new TestInput();
        var output = new TestOutput();

        var executor = CreateExecutor(input, output);

        var declaration =
            new VariableDeclarationNode(
                "sample",
                "numero",
                Sample(10),
                1,
                1);

        executor.ExecuteStatement(
            declaration,
            environment);

        Assert.Equal(
            10,
            environment.Resolve("numero").Value.Value);

        Assert.Equal(
            RuntimeValueKind.Sample,
            environment.Resolve("numero").DeclaredKind);
    }

    [Fact]
    public void ConstantDeclarationStoresConstantFlag()
    {
        var environment = new RuntimeEnvironment();
        var executor =
            CreateExecutor(
                new TestInput(),
                new TestOutput());

        var declaration =
            new ConstantDeclarationNode(
                "sample",
                "MAX",
                Sample(100),
                1,
                1);

        executor.ExecuteStatement(
            declaration,
            environment);

        Assert.True(
            environment.Resolve("MAX").IsConstant);
    }

    [Fact]
    public void AssignmentChangesVariableValue()
    {
        var environment = new RuntimeEnvironment();

        environment.Declare(
            "numero",
            RuntimeValue.Sample(10));

        var executor =
            CreateExecutor(
                new TestInput(),
                new TestOutput());

        var assignment =
            new AssignmentStatementNode(
                "numero",
                Sample(25),
                1,
                1);

        executor.ExecuteStatement(
            assignment,
            environment);

        Assert.Equal(
            25,
            environment.Resolve("numero").Value.Value);
    }

    [Fact]
    public void AssignmentToConstantThrows()
    {
        var environment = new RuntimeEnvironment();

        environment.Declare(
            "MAX",
            RuntimeValueKind.Sample,
            RuntimeValue.Sample(100),
            true);

        var executor =
            CreateExecutor(
                new TestInput(),
                new TestOutput());

        var assignment =
            new AssignmentStatementNode(
                "MAX",
                Sample(200),
                1,
                1);

        Assert.Throws<InvalidOperationException>(() =>
            executor.ExecuteStatement(
                assignment,
                environment));
    }

    [Fact]
    public void IncrementChangesSampleValue()
    {
        var environment = new RuntimeEnvironment();

        environment.Declare(
            "contador",
            RuntimeValue.Sample(5));

        var executor =
            CreateExecutor(
                new TestInput(),
                new TestOutput());

        var update =
            new UpdateStatementNode(
                "contador",
                TokenType.Increment,
                1,
                1);

        executor.ExecuteStatement(
            update,
            environment);

        Assert.Equal(
            6,
            environment.Resolve("contador").Value.Value);
    }

    [Fact]
    public void DecrementChangesSampleValue()
    {
        var environment = new RuntimeEnvironment();

        environment.Declare(
            "contador",
            RuntimeValue.Sample(5));

        var executor =
            CreateExecutor(
                new TestInput(),
                new TestOutput());

        var update =
            new UpdateStatementNode(
                "contador",
                TokenType.Decrement,
                1,
                1);

        executor.ExecuteStatement(
            update,
            environment);

        Assert.Equal(
            4,
            environment.Resolve("contador").Value.Value);
    }

    [Fact]
    public void OutputWritesWithoutAutomaticNewline()
    {
        var output = new TestOutput();

        var executor =
            CreateExecutor(
                new TestInput(),
                output);

        var statement =
            new OutputStatementNode(
                Phrase("Hola"),
                1,
                1);

        executor.ExecuteStatement(
            statement,
            new RuntimeEnvironment());

        Assert.Equal(
            "Hola",
            output.Text);
    }

    [Fact]
    public void OutputFormatsWholeWaveWithDecimalPart()
    {
        var output = new TestOutput();

        var executor =
            CreateExecutor(
                new TestInput(),
                output);

        var statement =
            new OutputStatementNode(
                Wave("3.0"),
                1,
                1);

        executor.ExecuteStatement(
            statement,
            new RuntimeEnvironment());

        Assert.Equal(
            "3.0",
            output.Text);
    }

    [Fact]
    public void InputParsesSample()
    {
        var environment = new RuntimeEnvironment();

        environment.Declare(
            "numero",
            RuntimeValueKind.Sample,
            RuntimeValue.Sample(0));

        var executor =
            CreateExecutor(
                new TestInput("42"),
                new TestOutput());

        executor.ExecuteStatement(
            new InputStatementNode(
                "numero",
                1,
                1),
            environment);

        Assert.Equal(
            42,
            environment.Resolve("numero").Value.Value);
    }

    [Fact]
    public void InputPreservesPhraseWhitespace()
    {
        var environment = new RuntimeEnvironment();

        environment.Declare(
            "texto",
            RuntimeValueKind.Phrase,
            RuntimeValue.Phrase(""));

        var executor =
            CreateExecutor(
                new TestInput("  hola TM  "),
                new TestOutput());

        executor.ExecuteStatement(
            new InputStatementNode(
                "texto",
                1,
                1),
            environment);

        Assert.Equal(
            "  hola TM  ",
            environment.Resolve("texto").Value.Value);
    }

    [Fact]
    public void InputParsesSignalIgnoringCase()
    {
        var environment = new RuntimeEnvironment();

        environment.Declare(
            "activo",
            RuntimeValueKind.Signal,
            RuntimeValue.Signal(false));

        var executor =
            CreateExecutor(
                new TestInput("TrUe"),
                new TestOutput());

        executor.ExecuteStatement(
            new InputStatementNode(
                "activo",
                1,
                1),
            environment);

        Assert.Equal(
            true,
            environment.Resolve("activo").Value.Value);
    }

    [Fact]
    public void InputReadsFirstNonWhitespaceTone()
    {
        var environment = new RuntimeEnvironment();

        environment.Declare(
            "nota",
            RuntimeValueKind.Tone,
            RuntimeValue.Tone('A'));

        var executor =
            CreateExecutor(
                new TestInput("   C"),
                new TestOutput());

        executor.ExecuteStatement(
            new InputStatementNode(
                "nota",
                1,
                1),
            environment);

        Assert.Equal(
            'C',
            environment.Resolve("nota").Value.Value);
    }

    [Fact]
    public void InvalidSampleInputProducesR004()
    {
        var environment = new RuntimeEnvironment();

        environment.Declare(
            "numero",
            RuntimeValueKind.Sample,
            RuntimeValue.Sample(0));

        var executor =
            CreateExecutor(
                new TestInput("hola"),
                new TestOutput());

        TmRuntimeException exception =
            Assert.Throws<TmRuntimeException>(() =>
                executor.ExecuteStatement(
                    new InputStatementNode(
                        "numero",
                        8,
                        1),
                    environment));

        Assert.Equal(
            RuntimeErrorCodes.InputParseError,
            exception.Code);
    }

    [Fact]
    public void EndOfInputProducesR006()
    {
        var environment = new RuntimeEnvironment();

        environment.Declare(
            "numero",
            RuntimeValueKind.Sample,
            RuntimeValue.Sample(0));

        var executor =
            CreateExecutor(
                new TestInput(),
                new TestOutput());

        TmRuntimeException exception =
            Assert.Throws<TmRuntimeException>(() =>
                executor.ExecuteStatement(
                    new InputStatementNode(
                        "numero",
                        4,
                        1),
                    environment));

        Assert.Equal(
            RuntimeErrorCodes.InputEndOfFile,
            exception.Code);
    }

    [Fact]
    public void EmptyToneInputProducesR007()
    {
        var environment = new RuntimeEnvironment();

        environment.Declare(
            "nota",
            RuntimeValueKind.Tone,
            RuntimeValue.Tone('A'));

        var executor =
            CreateExecutor(
                new TestInput("   "),
                new TestOutput());

        TmRuntimeException exception =
            Assert.Throws<TmRuntimeException>(() =>
                executor.ExecuteStatement(
                    new InputStatementNode(
                        "nota",
                        6,
                        1),
                    environment));

        Assert.Equal(
            RuntimeErrorCodes.InvalidToneInput,
            exception.Code);
    }

    private static StatementExecutor CreateExecutor(
        IRuntimeInput input,
        IRuntimeOutput output)
    {
        return new StatementExecutor(
            new ExpressionEvaluator(),
            input,
            output);
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

    private sealed class TestInput : IRuntimeInput
    {
        private readonly Queue<string> _lines = new();

        public TestInput(params string[] lines)
        {
            foreach (string line in lines)
            {
                _lines.Enqueue(line);
            }
        }

        public string? ReadLine()
        {
            return _lines.Count > 0
                ? _lines.Dequeue()
                : null;
        }
    }

    private sealed class TestOutput : IRuntimeOutput
    {
        public string Text { get; private set; } = string.Empty;

        public void Write(string text)
        {
            Text += text;
        }
    }
}