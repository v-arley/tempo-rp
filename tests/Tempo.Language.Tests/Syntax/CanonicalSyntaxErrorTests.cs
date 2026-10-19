using Tempo.Language.Diagnostics;
using Tempo.Language.Syntax;
using Xunit;

namespace Tempo.Language.Tests.Syntax;

public sealed class CanonicalSyntaxErrorTests
{
    [Theory]
    [InlineData(
        "session >\n    sample x = 10  # Falta ;\n<",
        DiagnosticCodes.MissingSemicolon,
        2)]
    [InlineData(
        "session <\n    output >> 1;\n>",
        DiagnosticCodes.MissingBlockDelimiter,
        1)]
    [InlineData(
        "session >\n    if x > 0 >\n        output >> 1;\n    <\n<",
        DiagnosticCodes.MissingConditionBrackets,
        2)]
    [InlineData(
        "compose funcion() >\n    ret;\n<",
        DiagnosticCodes.MalformedFunctionDeclaration,
        1)]
    public void CanonicalSyntaxError_ReportsExpectedCodeAndLine(
        string source,
        string expectedCode,
        int expectedLine)
    {
        SyntaxTree syntaxTree = SyntaxTree.Parse(
            source,
            "test.tmpr",
            "test.tmpr");

        Diagnostic diagnostic = Assert.Single(
            syntaxTree.Diagnostics);

        Assert.Equal(expectedCode, diagnostic.Code);
        Assert.Equal(expectedLine, diagnostic.Line);
        Assert.True(diagnostic.IsError);
    }
}