using System.Globalization;
using Tempo.Language.Evaluation;
using Tempo.Language.Syntax;
using Xunit;

namespace Tempo.Language.Tests.Evaluation;

public sealed class ControlFlowTests
{
    [Fact]
    public void IfExecutesThenBlockWhenConditionIsTrue()
    {
        var output = new TestOutput();
        StatementExecutor executor = CreateExecutor(output);

        var statement =
            new IfStatementNode(
                Signal(true),
                Block(
                    Output("then")),
                Array.Empty<ElseIfClauseNode>(),
                null,
                1,
                1);

        executor.ExecuteStatement(
            statement,
            new RuntimeEnvironment());

        Assert.Equal(
            "then",
            output.Text);
    }

    [Fact]
    public void IfExecutesElseBlockWhenConditionIsFalse()
    {
        var output = new TestOutput();
        StatementExecutor executor = CreateExecutor(output);

        var statement =
            new IfStatementNode(
                Signal(false),
                Block(
                    Output("then")),
                Array.Empty<ElseIfClauseNode>(),
                Block(
                    Output("else")),
                1,
                1);

        executor.ExecuteStatement(
            statement,
            new RuntimeEnvironment());

        Assert.Equal(
            "else",
            output.Text);
    }

    [Fact]
    public void IfExecutesFirstMatchingElseIf()
    {
        var output = new TestOutput();
        StatementExecutor executor = CreateExecutor(output);

        ElseIfClauseNode[] clauses =
        [
            new ElseIfClauseNode(
                Signal(false),
                Block(
                    Output("first"))),

            new ElseIfClauseNode(
                Signal(true),
                Block(
                    Output("second"))),

            new ElseIfClauseNode(
                Signal(true),
                Block(
                    Output("third")))
        ];

        var statement =
            new IfStatementNode(
                Signal(false),
                Block(
                    Output("then")),
                clauses,
                Block(
                    Output("else")),
                1,
                1);

        executor.ExecuteStatement(
            statement,
            new RuntimeEnvironment());

        Assert.Equal(
            "second",
            output.Text);
    }

    [Fact]
    public void RepeatExecutesWhileConditionIsTrue()
    {
        var environment = new RuntimeEnvironment();

        environment.Declare(
            "contador",
            RuntimeValue.Sample(0));

        var output = new TestOutput();
        StatementExecutor executor = CreateExecutor(output);

        var condition =
            new BinaryExpressionNode(
                Identifier("contador"),
                TokenType.LessThan,
                Sample(3),
                1,
                1);

        var repeat =
            new RepeatStatementNode(
                condition,
                Block(
                    new OutputStatementNode(
                        Identifier("contador"),
                        1,
                        1),
                    new UpdateStatementNode(
                        "contador",
                        TokenType.Increment,
                        1,
                        1)),
                1,
                1);

        executor.ExecuteStatement(
            repeat,
            environment);

        Assert.Equal(
            "012",
            output.Text);

        Assert.Equal(
            3,
            environment.Resolve("contador").Value.Value);
    }

    [Fact]
    public void LoopExecutesInitializerConditionUpdateAndBody()
    {
        var output = new TestOutput();
        StatementExecutor executor = CreateExecutor(output);

        var loop =
            new LoopStatementNode(
                new VariableDeclarationNode(
                    "sample",
                    "i",
                    Sample(0),
                    1,
                    1),
                new BinaryExpressionNode(
                    Identifier("i"),
                    TokenType.LessThan,
                    Sample(3),
                    1,
                    1),
                new LoopUpdateNode(
                    "i",
                    TokenType.Increment,
                    null,
                    1,
                    1),
                Block(
                    new OutputStatementNode(
                        Identifier("i"),
                        1,
                        1)),
                1,
                1);

        executor.ExecuteStatement(
            loop,
            new RuntimeEnvironment());

        Assert.Equal(
            "012",
            output.Text);
    }

    [Fact]
    public void LoopSupportsAssignmentUpdate()
    {
        var output = new TestOutput();
        StatementExecutor executor = CreateExecutor(output);

        var updateExpression =
            new BinaryExpressionNode(
                Identifier("i"),
                TokenType.Plus,
                Sample(2),
                1,
                1);

        var loop =
            new LoopStatementNode(
                new VariableDeclarationNode(
                    "sample",
                    "i",
                    Sample(0),
                    1,
                    1),
                new BinaryExpressionNode(
                    Identifier("i"),
                    TokenType.LessThan,
                    Sample(5),
                    1,
                    1),
                new LoopUpdateNode(
                    "i",
                    TokenType.Assign,
                    updateExpression,
                    1,
                    1),
                Block(
                    new OutputStatementNode(
                        Identifier("i"),
                        1,
                        1)),
                1,
                1);

        executor.ExecuteStatement(
            loop,
            new RuntimeEnvironment());

        Assert.Equal(
            "024",
            output.Text);
    }

    [Fact]
    public void LoopInitializerDoesNotEscapeLoopScope()
    {
        var environment = new RuntimeEnvironment();
        StatementExecutor executor =
            CreateExecutor(new TestOutput());

        var loop =
            new LoopStatementNode(
                new VariableDeclarationNode(
                    "sample",
                    "i",
                    Sample(0),
                    1,
                    1),
                new BinaryExpressionNode(
                    Identifier("i"),
                    TokenType.LessThan,
                    Sample(1),
                    1,
                    1),
                new LoopUpdateNode(
                    "i",
                    TokenType.Increment,
                    null,
                    1,
                    1),
                Block(),
                1,
                1);

        executor.ExecuteStatement(
            loop,
            environment);

        Assert.Throws<InvalidOperationException>(() =>
            environment.Resolve("i"));
    }

    [Fact]
    public void MixExecutesMatchingCase()
    {
        var output = new TestOutput();
        StatementExecutor executor = CreateExecutor(output);

        MixCaseNode[] cases =
        [
            new MixCaseNode(
                Sample(1),
                Block(
                    Output("one"))),

            new MixCaseNode(
                Sample(2),
                Block(
                    Output("two")))
        ];

        var mix =
            new MixStatementNode(
                Sample(2),
                cases,
                Block(
                    Output("default")),
                1,
                1);

        executor.ExecuteStatement(
            mix,
            new RuntimeEnvironment());

        Assert.Equal(
            "two",
            output.Text);
    }

    [Fact]
    public void MixExecutesDefaultWhenNoCaseMatches()
    {
        var output = new TestOutput();
        StatementExecutor executor = CreateExecutor(output);

        MixCaseNode[] cases =
        [
            new MixCaseNode(
                Sample(1),
                Block(
                    Output("one"))),

            new MixCaseNode(
                Sample(2),
                Block(
                    Output("two")))
        ];

        var mix =
            new MixStatementNode(
                Sample(9),
                cases,
                Block(
                    Output("default")),
                1,
                1);

        executor.ExecuteStatement(
            mix,
            new RuntimeEnvironment());

        Assert.Equal(
            "default",
            output.Text);
    }

    [Fact]
    public void MixDoesNotFallThroughAfterMatchingCase()
    {
        var output = new TestOutput();
        StatementExecutor executor = CreateExecutor(output);

        MixCaseNode[] cases =
        [
            new MixCaseNode(
                Sample(1),
                Block(
                    Output("A"))),

            new MixCaseNode(
                Sample(1),
                Block(
                    Output("B")))
        ];

        var mix =
            new MixStatementNode(
                Sample(1),
                cases,
                Block(
                    Output("default")),
                1,
                1);

        executor.ExecuteStatement(
            mix,
            new RuntimeEnvironment());

        Assert.Equal(
            "A",
            output.Text);
    }

    private static StatementExecutor CreateExecutor(
        IRuntimeOutput output)
    {
        return new StatementExecutor(
            new ExpressionEvaluator(),
            new EmptyInput(),
            output);
    }

    private static BlockNode Block(
        params StatementNode[] statements)
    {
        return new BlockNode(statements);
    }

    private static OutputStatementNode Output(
        string text)
    {
        return new OutputStatementNode(
            Phrase(text),
            1,
            1);
    }

    private static IdentifierExpressionNode Identifier(
        string name)
    {
        return new IdentifierExpressionNode(
            name,
            1,
            1);
    }

    private static LiteralExpressionNode Sample(
        int value)
    {
        return new LiteralExpressionNode(
            TokenType.IntegerLiteral,
            value.ToString(
                CultureInfo.InvariantCulture),
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

    private sealed class EmptyInput : IRuntimeInput
    {
        public string? ReadLine()
        {
            return null;
        }
    }

    private sealed class TestOutput : IRuntimeOutput
    {
        public string Text { get; private set; } =
            string.Empty;

        public void Write(string text)
        {
            Text += text;
        }
    }
}