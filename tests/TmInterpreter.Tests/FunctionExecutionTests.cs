using System.Globalization;
using TmCompiler.Domain.Lexical;
using TmCompiler.Domain.Syntactic;
using TmInterpreter.Application;
using TmInterpreter.Domain;
using Xunit;

namespace TmInterpreter.Tests;

public sealed class FunctionExecutionTests
{
    [Fact]
    public void FunctionReturnsCalculatedValue()
    {
        FunctionDeclarationNode function =
            new(
                "sample",
                "sumar",
                [
                    Parameter("sample", "a"),
                    Parameter("sample", "b")
                ],
                Block(
                    new ReturnStatementNode(
                        new BinaryExpressionNode(
                            Identifier("a"),
                            TokenType.Plus,
                            Identifier("b"),
                            1,
                            1),
                        1,
                        1)),
                1,
                1);

        FunctionInvoker runtime =
            CreateRuntime(function);

        RuntimeValue result =
            runtime.Invoke(
                Call(
                    "sumar",
                    Sample(3),
                    Sample(4)),
                new RuntimeEnvironment());

        Assert.Equal(
            RuntimeValueKind.Sample,
            result.Kind);

        Assert.Equal(
            7,
            result.Value);
    }

    [Fact]
    public void FunctionReceivesArgumentsFromCallerEnvironment()
    {
        FunctionDeclarationNode function =
            new(
                "sample",
                "doble",
                [
                    Parameter("sample", "numero")
                ],
                Block(
                    new ReturnStatementNode(
                        new BinaryExpressionNode(
                            Identifier("numero"),
                            TokenType.Multiply,
                            Sample(2),
                            1,
                            1),
                        1,
                        1)),
                1,
                1);

        FunctionInvoker runtime =
            CreateRuntime(function);

        var caller =
            new RuntimeEnvironment();

        caller.Declare(
            "valor",
            RuntimeValue.Sample(6));

        RuntimeValue result =
            runtime.Invoke(
                Call(
                    "doble",
                    Identifier("valor")),
                caller);

        Assert.Equal(
            12,
            result.Value);
    }

    [Fact]
    public void FunctionParametersDoNotModifyCallerVariables()
    {
        FunctionDeclarationNode function =
            new(
                "sample",
                "incrementar",
                [
                    Parameter("sample", "x")
                ],
                Block(
                    new UpdateStatementNode(
                        "x",
                        TokenType.Increment,
                        1,
                        1),
                    new ReturnStatementNode(
                        Identifier("x"),
                        1,
                        1)),
                1,
                1);

        FunctionInvoker runtime =
            CreateRuntime(function);

        var caller =
            new RuntimeEnvironment();

        caller.Declare(
            "x",
            RuntimeValue.Sample(10));

        RuntimeValue result =
            runtime.Invoke(
                Call(
                    "incrementar",
                    Identifier("x")),
                caller);

        Assert.Equal(
            11,
            result.Value);

        Assert.Equal(
            10,
            caller.Resolve("x").Value.Value);
    }

    [Fact]
    public void VoidFunctionCanBeCalledAsStatement()
    {
        var output = new TestOutput();

        FunctionDeclarationNode function =
            new(
                "void",
                "saludar",
                [
                    Parameter("phrase", "nombre")
                ],
                Block(
                    new OutputStatementNode(
                        new BinaryExpressionNode(
                            Phrase("Hola "),
                            TokenType.Plus,
                            Identifier("nombre"),
                            1,
                            1),
                        1,
                        1),
                    new ReturnStatementNode(
                        null,
                        1,
                        1)),
                1,
                1);

        FunctionInvoker runtime =
            CreateRuntime(
                output,
                function);

        var statement =
            new ExpressionStatementNode(
                Call(
                    "saludar",
                    Phrase("TM")),
                1,
                1);

        runtime.Statements.ExecuteStatement(
            statement,
            new RuntimeEnvironment());

        Assert.Equal(
            "Hola TM",
            output.Text);
    }

    [Fact]
    public void ReturnInsideIfExitsFunction()
    {
        FunctionDeclarationNode function =
            new(
                "sample",
                "seleccionar",
                [
                    Parameter("signal", "condicion")
                ],
                Block(
                    new IfStatementNode(
                        Identifier("condicion"),
                        Block(
                            new ReturnStatementNode(
                                Sample(10),
                                1,
                                1)),
                        Array.Empty<ElseIfClauseNode>(),
                        null,
                        1,
                        1),
                    new ReturnStatementNode(
                        Sample(20),
                        1,
                        1)),
                1,
                1);

        FunctionInvoker runtime =
            CreateRuntime(function);

        RuntimeValue result =
            runtime.Invoke(
                Call(
                    "seleccionar",
                    Signal(true)),
                new RuntimeEnvironment());

        Assert.Equal(
            10,
            result.Value);
    }

    [Fact]
    public void FunctionCanCallAnotherFunction()
    {
        FunctionDeclarationNode doubleFunction =
            new(
                "sample",
                "doble",
                [
                    Parameter("sample", "x")
                ],
                Block(
                    new ReturnStatementNode(
                        new BinaryExpressionNode(
                            Identifier("x"),
                            TokenType.Multiply,
                            Sample(2),
                            1,
                            1),
                        1,
                        1)),
                1,
                1);

        FunctionDeclarationNode quadrupleFunction =
            new(
                "sample",
                "cuadruple",
                [
                    Parameter("sample", "x")
                ],
                Block(
                    new ReturnStatementNode(
                        Call(
                            "doble",
                            Call(
                                "doble",
                                Identifier("x"))),
                        1,
                        1)),
                1,
                1);

        FunctionInvoker runtime =
            CreateRuntime(
                doubleFunction,
                quadrupleFunction);

        RuntimeValue result =
            runtime.Invoke(
                Call(
                    "cuadruple",
                    Sample(3)),
                new RuntimeEnvironment());

        Assert.Equal(
            12,
            result.Value);
    }

    [Fact]
    public void RecursiveFactorialReturnsExpectedValue()
    {
        var recursiveCall =
            Call(
                "factorial",
                new BinaryExpressionNode(
                    Identifier("n"),
                    TokenType.Minus,
                    Sample(1),
                    1,
                    1));

        FunctionDeclarationNode factorial =
            new(
                "sample",
                "factorial",
                [
                    Parameter("sample", "n")
                ],
                Block(
                    new IfStatementNode(
                        new BinaryExpressionNode(
                            Identifier("n"),
                            TokenType.LessOrEqual,
                            Sample(1),
                            1,
                            1),
                        Block(
                            new ReturnStatementNode(
                                Sample(1),
                                1,
                                1)),
                        Array.Empty<ElseIfClauseNode>(),
                        null,
                        1,
                        1),
                    new ReturnStatementNode(
                        new BinaryExpressionNode(
                            Identifier("n"),
                            TokenType.Multiply,
                            recursiveCall,
                            1,
                            1),
                        1,
                        1)),
                1,
                1);

        FunctionInvoker runtime =
            CreateRuntime(factorial);

        RuntimeValue result =
            runtime.Invoke(
                Call(
                    "factorial",
                    Sample(5)),
                new RuntimeEnvironment());

        Assert.Equal(
            120,
            result.Value);

        Assert.Equal(
            5,
            runtime.CallStack.MaximumDepth);

        Assert.Equal(
            0,
            runtime.CallStack.Depth);
    }

    [Fact]
    public void UndefinedFunctionProducesR005()
    {
        FunctionInvoker runtime =
            CreateRuntime();

        TmRuntimeException exception =
            Assert.Throws<TmRuntimeException>(() =>
                runtime.Invoke(
                    Call(
                        "noExiste"),
                    new RuntimeEnvironment()));

        Assert.Equal(
            RuntimeErrorCodes.UndefinedFunction,
            exception.Code);
    }

    [Fact]
    public void CallStackIsEmptyAfterFunctionReturns()
    {
        FunctionDeclarationNode function =
            new(
                "sample",
                "identidad",
                [
                    Parameter("sample", "x")
                ],
                Block(
                    new ReturnStatementNode(
                        Identifier("x"),
                        1,
                        1)),
                1,
                1);

        FunctionInvoker runtime =
            CreateRuntime(function);

        runtime.Invoke(
            Call(
                "identidad",
                Sample(10)),
            new RuntimeEnvironment());

        Assert.Equal(
            0,
            runtime.CallStack.Depth);
    }

    [Fact]
    public void VoidFunctionReturnsVoidWhenBodyEnds()
    {
        FunctionDeclarationNode function =
            new(
                "void",
                "vacia",
                Array.Empty<ParameterNode>(),
                Block(),
                1,
                1);

        FunctionInvoker runtime =
            CreateRuntime(function);

        RuntimeValue result =
            runtime.Invoke(
                Call("vacia"),
                new RuntimeEnvironment());

        Assert.Equal(
            RuntimeValueKind.Void,
            result.Kind);
    }

    private static FunctionInvoker CreateRuntime(
        params FunctionDeclarationNode[] functions)
    {
        return CreateRuntime(
            new TestOutput(),
            functions);
    }

    private static FunctionInvoker CreateRuntime(
        IRuntimeOutput output,
        params FunctionDeclarationNode[] functions)
    {
        return new FunctionInvoker(
            new FunctionRegistry(functions),
            new EmptyInput(),
            output);
    }

    private static ParameterNode Parameter(
        string type,
        string identifier)
    {
        return new ParameterNode(
            type,
            identifier,
            1,
            1);
    }

    private static BlockNode Block(
        params StatementNode[] statements)
    {
        return new BlockNode(statements);
    }

    private static CallExpressionNode Call(
        string identifier,
        params ExpressionNode[] arguments)
    {
        return new CallExpressionNode(
            identifier,
            arguments,
            1,
            1);
    }

    private static IdentifierExpressionNode Identifier(
        string identifier)
    {
        return new IdentifierExpressionNode(
            identifier,
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