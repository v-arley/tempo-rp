using Tempo.Language.Diagnostics;
using Tempo.Language.Syntax;
using Xunit;

namespace Tempo.Language.Tests.Syntax;

public sealed class LexicalDiagnosticCodeTests
{
    [Theory]
    [InlineData(
        "@",
        DiagnosticCodes.InvalidCharacter)]
    [InlineData(
        "\"sin cerrar",
        DiagnosticCodes.UnterminatedString)]
    [InlineData(
        "'ab'",
        DiagnosticCodes.MalformedCharacterLiteral)]
    [InlineData(
        "\"\\q\"",
        DiagnosticCodes.InvalidEscapeSequence)]
    [InlineData(
        "## comentario sin cerrar",
        DiagnosticCodes.UnterminatedBlockComment)]
    [InlineData(
        "3.",
        DiagnosticCodes.MalformedNumericLiteral)]
    public void InvalidSource_ReportsExpectedLexicalCode(
        string source,
        string expectedCode)
    {
        var lexer = new Lexer(source, "test.tmpr");

        LexerException exception =
            Assert.Throws<LexerException>(
                () => lexer.Tokenize().ToList());

        Assert.Equal(expectedCode, exception.Code);
        Assert.Equal("test.tmpr", exception.FileName);
        Assert.Equal(1, exception.Line);
    }
}