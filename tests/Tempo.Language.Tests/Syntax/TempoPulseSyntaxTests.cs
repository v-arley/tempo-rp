using Tempo.Language.Diagnostics;
using Tempo.Language.Syntax;
using Tempo.Language.Tests.Binding;
using Xunit;

namespace Tempo.Language.Tests.Syntax;

/// <summary>
/// Sintaxis de la innovación: la declaración <c>tempo</c> y la
/// sentencia <c>pulse</c>.
/// </summary>

public sealed class TempoPulseSyntaxTests
{
    [Fact]
    public void Tempo_AfterImports_IsStoredInProgram()
    {
        ProgramNode program = SemanticTestHarness.Parse(
            "import \"utilidades.tm\";\ntempo 96;\nsession > <");

        Assert.NotNull(program.Tempo);
        Assert.Equal("96", program.Tempo.Value);
        Assert.Equal(2, program.Tempo.Line);
        Assert.Empty(program.AdditionalTempos);
    }

    [Fact]
    public void Tempo_Missing_LeavesNull()
    {
        ProgramNode program = SemanticTestHarness.Parse("session > <");

        Assert.Null(program.Tempo);
        Assert.Empty(program.AllTempos);
    }

    [Fact]
    public void Tempo_Repeated_KeepsTheRestForTheProjectRule()
    {
        ProgramNode program = SemanticTestHarness.Parse("tempo 96;\ntempo 120;\nsession > <");

        Assert.Equal("96", program.Tempo.Value);
        TempoDeclarationNode duplicate = Assert.Single(program.AdditionalTempos);
        Assert.Equal(2, duplicate.Line);
    }

    [Fact]
    public void Pulse_InsideBlock_ParsesIntensityExpression()
    {
        ProgramNode program = SemanticTestHarness.Parse("session >\n    pulse [40 + 4];\n<");

        PulseStatementNode pulse = Assert.IsType<PulseStatementNode>(
            Assert.Single(program.Session.Body.Statements));

        Assert.IsType<BinaryExpressionNode>(pulse.Intensity);
        Assert.Equal(2, pulse.Line);
    }

    [Theory]

    // tempo sin ';'
    [InlineData("tempo 96\nsession > <", DiagnosticCodes.MissingSemicolon, 2)]

    // tempo con un valor que no es un entero
    [InlineData("tempo 96.5;\nsession > <", DiagnosticCodes.UnexpectedTopLevelElement, 1)]

    // tempo después de una función
    [InlineData(
        "void : compose f() >\n    ret;\n<\ntempo 96;\nsession > <",
        DiagnosticCodes.UnexpectedTopLevelElement,
        4)]

    // tempo dentro de un bloque
    [InlineData("session >\n    tempo 96;\n<", DiagnosticCodes.InvalidStatement, 2)]

    // pulse sin corchetes
    [InlineData("session >\n    pulse 50;\n<", DiagnosticCodes.MissingConditionBrackets, 2)]

    // pulse sin ']'
    [InlineData("session >\n    pulse [50;\n<", DiagnosticCodes.MissingConditionBrackets, 2)]

    // pulse sin ';'
    [InlineData("session >\n    pulse [50]\n<", DiagnosticCodes.MissingSemicolon, 2)]

    public void InvalidSyntax_ReportsExpectedCodeAndLine(
        string source,
        string expectedCode,
        int expectedLine)
    {
        SyntaxTree syntaxTree = SyntaxTree.Parse(source, "test.tmpr", "test.tmpr");

        Diagnostic diagnostic = Assert.Single(syntaxTree.Diagnostics);

        Assert.Equal(expectedCode, diagnostic.Code);
        Assert.Equal(expectedLine, diagnostic.Line);
    }
}
