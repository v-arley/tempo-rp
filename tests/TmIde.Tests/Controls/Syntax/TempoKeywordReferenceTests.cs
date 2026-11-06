
using System;
using System.Linq;

using ICSharpCode.AvalonEdit.Document;

using TmIde.Controls.Syntax;

using Xunit;

namespace TmIde.Tests.Controls.Syntax;

public sealed class TempoKeywordReferenceTests
{
    [Fact]
    public void Catalog_ContainsAllThirtyWords()
    {
        Assert.Equal(30, TempoKeywordReference.Count);
    }

    [Theory]
    [InlineData("sample", "int")]
    [InlineData("wave", "float")]
    [InlineData("phrase", "string")]
    [InlineData("signal", "bool")]
    [InlineData("session", "main() / Main()")]
    [InlineData("mix", "switch")]
    [InlineData("loop", "for")]
    [InlineData("repeat", "while")]
    [InlineData("ret", "return")]
    [InlineData("and", "&&")]
    [InlineData("or", "||")]
    [InlineData("not", "!")]
    public void Keyword_HasExpectedEquivalent(
        string keyword,
        string expected)
    {
        var result = TempoKeywordReference.Find(keyword);

        Assert.NotNull(result);
        Assert.Equal(expected, result.Equivalent);
    }

    [Fact]
    public void AllKeywords_HaveInformation()
    {
        string[] words =
        [
            "sample", "wave", "tone", "phrase", "signal",
            "silence", "void", "sequence", "score",
            "session", "compose", "ret", "import", "const",
            "output", "input", "if", "else", "mix",
            "case", "default", "loop", "repeat",
            "and", "or", "not", "true", "false",
            "tempo", "pulse"
        ];

        Assert.Equal(30, words.Length);

        foreach (var word in words)
        {
            var info = TempoKeywordReference.Find(word);

            Assert.NotNull(info);
            Assert.Equal(word, info.Keyword);
            Assert.False(
                string.IsNullOrWhiteSpace(info.Equivalent));
            Assert.False(
                string.IsNullOrWhiteSpace(info.Description));
        }
    }

    [Fact]
    public void UnknownWord_ReturnsNull()
    {
        Assert.Null(TempoKeywordReference.Find("numero"));
        Assert.Null(TempoKeywordReference.Find("sumar"));
    }

    [Fact]
    public void Keywords_AreCaseSensitive()
    {
        Assert.NotNull(TempoKeywordReference.Find("sample"));

        Assert.Null(TempoKeywordReference.Find("Sample"));
        Assert.Null(TempoKeywordReference.Find("SAMPLE"));
    }

    [Fact]
    public void Scanner_RecognizesEveryDocumentedKeyword()
    {
        string[] words =
        [
            "sample", "wave", "tone", "phrase", "signal",
            "silence", "void", "sequence", "score",
            "session", "compose", "ret", "import", "const",
            "output", "input", "if", "else", "mix",
            "case", "default", "loop", "repeat",
            "and", "or", "not", "true", "false",
            "tempo", "pulse"
        ];

        var source = string.Join(" ", words);

        var spans = TmSyntaxScanner.Scan(source);

        Assert.Equal(words.Length, spans.Count);

        foreach (var span in spans)
        {
            var token = source.Substring(
                span.Start,
                span.Length);

            Assert.NotNull(TempoKeywordReference.Find(token));
        }
    }

    [Fact]
    public void Hover_RecognizesKeywordInCode()
    {
        const string source = "sample numero = 10;";

        var document = new TextDocument(source);
        var colorizer = new TmSyntaxColorizer();

        colorizer.Rescan(document);

        Assert.Equal(
            "sample",
            colorizer.GetReservedWordAt(document, 2));
    }

    [Fact]
    public void Hover_IgnoresKeywordsInsideComments()
    {
        const string source =
            "sample numero = 10; # repeat";

        var document = new TextDocument(source);
        var colorizer = new TmSyntaxColorizer();

        colorizer.Rescan(document);

        var position = source.IndexOf(
            "repeat",
            StringComparison.Ordinal);

        Assert.Null(
            colorizer.GetReservedWordAt(document, position));
    }

    [Fact]
    public void Hover_IgnoresKeywordsInsideStrings()
    {
        const string source =
            "phrase texto = \"session\";";

        var document = new TextDocument(source);
        var colorizer = new TmSyntaxColorizer();

        colorizer.Rescan(document);

        var position = source.IndexOf(
            "session",
            StringComparison.Ordinal);

        Assert.Null(
            colorizer.GetReservedWordAt(document, position));
    }

    [Fact]
    public void Hover_IgnoresSimilarIdentifiers()
    {
        const string source =
            "sample sampleTotal = 10;";

        var document = new TextDocument(source);
        var colorizer = new TmSyntaxColorizer();

        colorizer.Rescan(document);

        var position = source.IndexOf(
            "sampleTotal",
            StringComparison.Ordinal);

        Assert.Null(
            colorizer.GetReservedWordAt(document, position));
    }

    [Fact]
    public void Hover_RecognizesCaseAndDefault()
    {
        const string source =
            "case 1: > output >> 1; < default: > output >> 0; <";

        var document = new TextDocument(source);
        var colorizer = new TmSyntaxColorizer();

        colorizer.Rescan(document);

        var casePosition = source.IndexOf(
            "case",
            StringComparison.Ordinal);

        var defaultPosition = source.IndexOf(
            "default",
            StringComparison.Ordinal);

        Assert.Equal(
            "case",
            colorizer.GetReservedWordAt(
                document,
                casePosition));

        Assert.Equal(
            "default",
            colorizer.GetReservedWordAt(
                document,
                defaultPosition));
    }

    [Fact]
    public void Hover_RejectsPositionOutsideDocument()
    {
        var document = new TextDocument("sample");
        var colorizer = new TmSyntaxColorizer();

        colorizer.Rescan(document);

        Assert.Null(
            colorizer.GetReservedWordAt(document, -1));

        Assert.Null(
            colorizer.GetReservedWordAt(
                document,
                document.TextLength));
    }
}
