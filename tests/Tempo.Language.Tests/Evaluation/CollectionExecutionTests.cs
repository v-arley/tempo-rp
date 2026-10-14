using System.Globalization;
using Tempo.Language.Evaluation;
using Tempo.Language.Syntax;
using Xunit;

namespace Tempo.Language.Tests.Evaluation;

public sealed class CollectionExecutionTests
{
    [Fact]
    public void EvaluatesSequenceLiteral()
    {
        var evaluator =
            new ExpressionEvaluator();

        RuntimeValue result =
            evaluator.Evaluate(
                Collection(
                    Sample(10),
                    Sample(20),
                    Sample(30)),
                new RuntimeEnvironment());

        Assert.Equal(
            RuntimeValueKind.Sequence,
            result.Kind);

        RuntimeSequence sequence =
            Assert.IsType<RuntimeSequence>(
                result.Value);

        Assert.Equal(
            3,
            sequence.Count);

        Assert.Equal(
            20,
            sequence.Get(1).Value);
    }

    [Fact]
    public void EvaluatesScoreLiteral()
    {
        var evaluator =
            new ExpressionEvaluator();

        RuntimeValue result =
            evaluator.Evaluate(
                Collection(
                    Collection(
                        Sample(1),
                        Sample(2)),
                    Collection(
                        Sample(3),
                        Sample(4))),
                new RuntimeEnvironment());

        Assert.Equal(
            RuntimeValueKind.Score,
            result.Kind);

        RuntimeScore score =
            Assert.IsType<RuntimeScore>(
                result.Value);

        Assert.Equal(
            2,
            score.RowCount);

        Assert.Equal(
            4,
            score.GetRow(1).Get(1).Value);
    }

    [Fact]
    public void ReadsSequenceIndex()
    {
        RuntimeEnvironment environment =
            EnvironmentWithSequence();

        var evaluator =
            new ExpressionEvaluator();

        RuntimeValue result =
            evaluator.Evaluate(
                Index(
                    Identifier("nums"),
                    Sample(1)),
                environment);

        Assert.Equal(
            20,
            result.Value);
    }

    [Fact]
    public void ReadsNestedScoreIndex()
    {
        RuntimeEnvironment environment =
            EnvironmentWithScore();

        var evaluator =
            new ExpressionEvaluator();

        RuntimeValue result =
            evaluator.Evaluate(
                Index(
                    Index(
                        Identifier("matriz"),
                        Sample(1)),
                    Sample(0)),
                environment);

        Assert.Equal(
            3,
            result.Value);
    }

    [Fact]
    public void SequenceLengthReturnsElementCount()
    {
        RuntimeEnvironment environment =
            EnvironmentWithSequence();

        var evaluator =
            new ExpressionEvaluator();

        RuntimeValue result =
            evaluator.Evaluate(
                new LengthExpressionNode(
                    Identifier("nums"),
                    1,
                    1),
                environment);

        Assert.Equal(
            RuntimeValueKind.Sample,
            result.Kind);

        Assert.Equal(
            3,
            result.Value);
    }

    [Fact]
    public void ScoreLengthReturnsRowCount()
    {
        RuntimeEnvironment environment =
            EnvironmentWithScore();

        var evaluator =
            new ExpressionEvaluator();

        RuntimeValue result =
            evaluator.Evaluate(
                new LengthExpressionNode(
                    Identifier("matriz"),
                    1,
                    1),
                environment);

        Assert.Equal(
            2,
            result.Value);
    }

    [Fact]
    public void AssignsSequenceElement()
    {
        RuntimeEnvironment environment =
            EnvironmentWithSequence();

        StatementExecutor executor =
            CreateExecutor();

        executor.ExecuteStatement(
            new IndexedAssignmentStatementNode(
                Index(
                    Identifier("nums"),
                    Sample(1)),
                Sample(99),
                1,
                1),
            environment);

        RuntimeSequence sequence =
            (RuntimeSequence)
            environment.Resolve("nums")
                .Value.Value!;

        Assert.Equal(
            99,
            sequence.Get(1).Value);
    }

    [Fact]
    public void AssignsScoreElement()
    {
        RuntimeEnvironment environment =
            EnvironmentWithScore();

        StatementExecutor executor =
            CreateExecutor();

        executor.ExecuteStatement(
            new IndexedAssignmentStatementNode(
                Index(
                    Index(
                        Identifier("matriz"),
                        Sample(0)),
                    Sample(1)),
                Sample(9),
                1,
                1),
            environment);

        RuntimeScore score =
            (RuntimeScore)
            environment.Resolve("matriz")
                .Value.Value!;

        Assert.Equal(
            9,
            score.GetRow(0).Get(1).Value);
    }

    [Fact]
    public void SequenceAssignmentCopiesReference()
    {
        RuntimeEnvironment environment =
            EnvironmentWithSequence();

        environment.Declare(
            "copia",
            RuntimeValueKind.Sequence,
            RuntimeValue.Sequence(
                new RuntimeSequence(
                    [RuntimeValue.Sample(0)])));

        StatementExecutor executor =
            CreateExecutor();

        executor.ExecuteStatement(
            new AssignmentStatementNode(
                "copia",
                Identifier("nums"),
                1,
                1),
            environment);

        executor.ExecuteStatement(
            new IndexedAssignmentStatementNode(
                Index(
                    Identifier("copia"),
                    Sample(0)),
                Sample(77),
                1,
                1),
            environment);

        RuntimeSequence original =
            (RuntimeSequence)
            environment.Resolve("nums")
                .Value.Value!;

        Assert.Equal(
            77,
            original.Get(0).Value);
    }

    [Fact]
    public void ScoreAssignmentCopiesReference()
    {
        RuntimeEnvironment environment =
            EnvironmentWithScore();

        environment.Declare(
            "copia",
            RuntimeValueKind.Score,
            RuntimeValue.Score(
                new RuntimeScore(
                    [
                        new RuntimeSequence(
                            [RuntimeValue.Sample(0)])
                    ])));

        StatementExecutor executor =
            CreateExecutor();

        executor.ExecuteStatement(
            new AssignmentStatementNode(
                "copia",
                Identifier("matriz"),
                1,
                1),
            environment);

        executor.ExecuteStatement(
            new IndexedAssignmentStatementNode(
                Index(
                    Index(
                        Identifier("copia"),
                        Sample(1)),
                    Sample(1)),
                Sample(88),
                1,
                1),
            environment);

        RuntimeScore original =
            (RuntimeScore)
            environment.Resolve("matriz")
                .Value.Value!;

        Assert.Equal(
            88,
            original.GetRow(1).Get(1).Value);
    }

    [Fact]
    public void ReadingOutsideSequenceProducesR002()
    {
        RuntimeEnvironment environment =
            EnvironmentWithSequence();

        var evaluator =
            new ExpressionEvaluator();

        TmRuntimeException exception =
            Assert.Throws<TmRuntimeException>(() =>
                evaluator.Evaluate(
                    Index(
                        Identifier("nums"),
                        Sample(99)),
                    environment));

        Assert.Equal(
            RuntimeErrorCodes.IndexOutOfBounds,
            exception.Code);
    }

    [Fact]
    public void WritingOutsideSequenceProducesR002()
    {
        RuntimeEnvironment environment =
            EnvironmentWithSequence();

        StatementExecutor executor =
            CreateExecutor();

        TmRuntimeException exception =
            Assert.Throws<TmRuntimeException>(() =>
                executor.ExecuteStatement(
                    new IndexedAssignmentStatementNode(
                        Index(
                            Identifier("nums"),
                            Sample(-1)),
                        Sample(10),
                        1,
                        1),
                    environment));

        Assert.Equal(
            RuntimeErrorCodes.IndexOutOfBounds,
            exception.Code);
    }

    private static RuntimeEnvironment EnvironmentWithSequence()
    {
        var environment =
            new RuntimeEnvironment();

        environment.Declare(
            "nums",
            RuntimeValueKind.Sequence,
            RuntimeValue.Sequence(
                new RuntimeSequence(
                    [
                        RuntimeValue.Sample(10),
                        RuntimeValue.Sample(20),
                        RuntimeValue.Sample(30)
                    ])));

        return environment;
    }

    private static RuntimeEnvironment EnvironmentWithScore()
    {
        var environment =
            new RuntimeEnvironment();

        environment.Declare(
            "matriz",
            RuntimeValueKind.Score,
            RuntimeValue.Score(
                new RuntimeScore(
                    [
                        new RuntimeSequence(
                            [
                                RuntimeValue.Sample(1),
                                RuntimeValue.Sample(2)
                            ]),
                        new RuntimeSequence(
                            [
                                RuntimeValue.Sample(3),
                                RuntimeValue.Sample(4)
                            ])
                    ])));

        return environment;
    }

    private static StatementExecutor CreateExecutor()
    {
        return new StatementExecutor(
            new ExpressionEvaluator(),
            new EmptyInput(),
            new EmptyOutput());
    }

    private static CollectionLiteralExpressionNode Collection(
        params ExpressionNode[] elements)
    {
        return new CollectionLiteralExpressionNode(
            elements,
            1,
            1);
    }

    private static IndexExpressionNode Index(
        ExpressionNode target,
        ExpressionNode index)
    {
        return new IndexExpressionNode(
            target,
            index,
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

    private sealed class EmptyInput : IRuntimeInput
    {
        public string? ReadLine()
        {
            return null;
        }
    }

    private sealed class EmptyOutput : IRuntimeOutput
    {
        public void Write(string text)
        {
        }
    }
}