using Tempo.Language.Evaluation;
using Tempo.Language.Syntax;
using Xunit;

namespace Tempo.Language.Tests.Evaluation;

/// <summary>
/// <c>ret;</c> dentro de <c>session</c> termina el programa con normalidad,
/// sin ejecutar las sentencias que le siguen.
/// </summary>

public sealed class SessionReturnTests
{
    [Fact]
    public void Execute_ReturnInSession_SkipsRemainingStatements()
    {
        string output = Execute(
            """
            session >
                output >> "a";
                ret;
                output >> "b";
            <
            """);

        Assert.Equal("a", output);
    }

    [Fact]
    public void Execute_ReturnInsideLoopOfSession_EndsTheProgram()
    {
        string output = Execute(
            """
            session >
                loop [sample i = 0; i < 5; i++] >
                    if [i == 2] >
                        ret;
                    <
                    output >> i;
                <
                output >> "fin";
            <
            """);

        Assert.Equal("01", output);
    }

    [Fact]
    public void Execute_ReturnInFunctionCalledFromSession_ContinuesTheSession()
    {
        string output = Execute(
            """
            void : compose saludar() >
                output >> "hola ";
                ret;
            <

            session >
                saludar();
                output >> "mundo";
            <
            """);

        Assert.Equal("hola mundo", output);
    }

    private static string Execute(string source)
    {
        ProgramNode program = new Parser(
            new Lexer(source, "session.tmpr").Tokenize(),
            "session.tmpr").Parse();

        var output = new TestOutput();

        new Interpreter(program, new EmptyInput(), output).Execute();

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
        public string Text { get; private set; } = string.Empty;

        public void Write(string text)
        {
            Text += text;
        }
    }
}
