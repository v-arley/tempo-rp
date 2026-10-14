using Tempo.Language.Evaluation;
using Tempo.Language.Syntax;
using Xunit;

namespace Tempo.Language.Tests.Evaluation;

public sealed class CanonicalProgramTests
{
    [Fact]
    public void V1ExecutesBasicArithmetic()
    {
        const string source = """
            session >
                sample a = 10;
                sample b = 3;
                sample suma = a + b;
                sample resta = a - b;
                sample mult = a * b;
                sample div = a / b;
                output >> "Suma: " + suma;
                output >> "Resta: " + resta;
                output >> "Mult: " + mult;
                output >> "Div: " + div;
            <
            """;

        string output =
            Execute(source);

        Assert.Equal(
            "Suma: 13Resta: 7Mult: 30Div: 3",
            output);
    }

    [Fact]
    public void V2ExecutesNestedConditionals()
    {
        const string source = """
            session >
                sample x = 15;

                if [x > 10] >
                    if [x < 20] >
                        output >> "Entre 10 y 20";
                    <
                <
            <
            """;

        string output =
            Execute(source);

        Assert.Equal(
            "Entre 10 y 20",
            output);
    }

    [Fact]
    public void V3ExecutesRecursiveFibonacci()
    {
        const string source = """
            sample : compose fibonacci(sample n) >
                if [n <= 1] >
                    ret n;
                <

                ret fibonacci(n - 1) + fibonacci(n - 2);
            <

            session >
                sample f = fibonacci(7);
                output >> "Fib(7) = " + f;
            <
            """;

        string output =
            Execute(source);

        Assert.Equal(
            "Fib(7) = 13",
            output);
    }

    [Fact]
    public void V4ManipulatesSequences()
    {
        const string source = """
            session >
                sequence<sample> nums = [5, 10, 15, 20];
                sample i = 0;

                repeat [i < nums.length()] >
                    nums[i] = nums[i] * 2;
                    i++;
                <

                output >> nums[0];
                output >> nums[3];
            <
            """;

        string output =
            Execute(source);

        Assert.Equal(
            "1040",
            output);
    }

    [Fact]
    public void V5UsesConstantsAndMixedConcatenation()
    {
        const string source = """
            session >
                const sample LIMITE = 100;
                const phrase MENSAJE = "El limite es: ";
                output >> MENSAJE + LIMITE;
            <
            """;

        string output =
            Execute(source);

        Assert.Equal(
            "El limite es: 100",
            output);
    }

    private static string Execute(
        string source)
    {
        var lexer =
            new Lexer(
                source,
                "canonical.tmpr");

        var parser =
            new Parser(
                lexer.Tokenize(),
                "canonical.tmpr");

        ProgramNode program =
            parser.Parse();

        var output =
            new TestOutput();

        var interpreter =
            new Interpreter(
                program,
                new EmptyInput(),
                output);

        interpreter.Execute();

        return output.Text;
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