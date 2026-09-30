using TmCompiler.Application.Lexing;
using TmCompiler.Application.Parsing;
using TmCompiler.Domain.Syntactic;
using TmInterpreter.Application;
using TmInterpreter.Domain;
using Xunit;

namespace TmInterpreter.Tests;

public sealed class RuntimeErrorContractTests
{
    [Fact]
    public void R001DivisionByZero()
    {
        const string source = """
            session >
                sample x = 10 / 0;
            <
            """;

        TmRuntimeException exception =
            ExecuteExpectingError(source);

        Assert.Equal(
            "R001",
            exception.Code);
    }

    [Fact]
    public void R002IndexOutsideBounds()
    {
        const string source = """
            session >
                sequence<sample> nums = [10, 20];
                output >> nums[2];
            <
            """;

        TmRuntimeException exception =
            ExecuteExpectingError(source);

        Assert.Equal(
            "R002",
            exception.Code);
    }

    [Fact]
    public void R003ArithmeticOverflow()
    {
        const string source = """
            session >
                sample maximo = 2147483647;
                sample resultado = maximo + 1;
            <
            """;

        TmRuntimeException exception =
            ExecuteExpectingError(source);

        Assert.Equal(
            "R003",
            exception.Code);
    }

    [Fact]
    public void R004InvalidInputParsing()
    {
        const string source = """
            session >
                sample numero = 0;
                input << numero;
            <
            """;

        TmRuntimeException exception =
            ExecuteExpectingError(
                source,
                "esto-no-es-un-numero");

        Assert.Equal(
            "R004",
            exception.Code);
    }

    [Fact]
    public void R005UndefinedFunction()
    {
        const string source = """
            session >
                noExiste();
            <
            """;

        TmRuntimeException exception =
            ExecuteExpectingError(source);

        Assert.Equal(
            "R005",
            exception.Code);
    }

    [Fact]
    public void R006InputEndOfFile()
    {
        const string source = """
            session >
                sample numero = 0;
                input << numero;
            <
            """;

        TmRuntimeException exception =
            ExecuteExpectingError(source);

        Assert.Equal(
            "R006",
            exception.Code);
    }

    [Fact]
    public void R007InvalidToneInput()
    {
        const string source = """
            session >
                tone nota = 'C';
                input << nota;
            <
            """;

        TmRuntimeException exception =
            ExecuteExpectingError(
                source,
                "   ");

        Assert.Equal(
            "R007",
            exception.Code);
    }

    private static TmRuntimeException ExecuteExpectingError(
        string source,
        params string[] inputLines)
    {
        ProgramNode program =
            Parse(source);

        var interpreter =
            new Interpreter(
                program,
                new QueueInput(inputLines),
                new EmptyOutput());

        return Assert.Throws<TmRuntimeException>(
            () => interpreter.Execute());
    }

    private static ProgramNode Parse(
        string source)
    {
        var lexer =
            new Lexer(
                source,
                "runtime-error.tmpr");

        var parser =
            new Parser(
                lexer.Tokenize(),
                "runtime-error.tmpr");

        return parser.Parse();
    }

    private sealed class QueueInput : IRuntimeInput
    {
        private readonly Queue<string> _lines;

        public QueueInput(
            IEnumerable<string> lines)
        {
            _lines =
                new Queue<string>(lines);
        }

        public string? ReadLine()
        {
            return _lines.Count == 0
                ? null
                : _lines.Dequeue();
        }
    }

    private sealed class EmptyOutput : IRuntimeOutput
    {
        public void Write(string text)  
        {
        }
    }
}