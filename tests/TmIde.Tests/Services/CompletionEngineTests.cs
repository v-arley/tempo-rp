using System.Linq;

using TmIde.Services.Completion;

using Xunit;

namespace TmIde.Tests.Services;

/// <summary>
/// Autocompletado sin símbolos: el contexto del cursor decide si se sugiere
/// algo, y el motor filtra y ordena el vocabulario y las plantillas.
/// </summary>

public sealed class CompletionEngineTests
{
    private const string Caret = "|";

    private static readonly CompletionEngine Engine = CompletionEngine.WithoutSymbols();

    // El texto se escribe con '|' donde está el cursor.
    private static CompletionContext ContextOf(string textWithCaret) =>
        CompletionContext.At(textWithCaret.Replace(Caret, string.Empty, System.StringComparison.Ordinal),textWithCaret.IndexOf(Caret, System.StringComparison.Ordinal));

    private static string[] LabelsFor(string textWithCaret) =>
        Engine.SuggestionsFor(ContextOf(textWithCaret)).Select(suggestion => suggestion.Label).Distinct().ToArray();

    [Fact]
    public void Context_ReadsThePrefixLineAndIndentation()
    {
        var context = ContextOf("session >\n    sam|");

        Assert.Equal("sam", context.Prefix);
        Assert.Equal(2, context.Line);
        Assert.Equal("    ", context.Indentation);
        Assert.False(context.IsSilent);
    }

    [Theory]
    [InlineData("# comentario sa|")]
    [InlineData("## bloque sa|")]
    [InlineData("## bloque sa| ##")]
    [InlineData("output >> \"texto sa|")]
    [InlineData("output >> \"texto sa|\";")]
    [InlineData("sample x = 12|")]
    [InlineData("sample ed|")]
    [InlineData("sequence<sample> no|")]
    [InlineData("void : compose su|")]
    public void Context_IsSilentWhereSuggestionsGetInTheWay(string textWithCaret)
    {
        Assert.True(ContextOf(textWithCaret).IsSilent);
        Assert.Empty(LabelsFor(textWithCaret));
    }

    [Theory]
    [InlineData("# comentario\nsa|")]
    [InlineData("## bloque ## sa|")]
    [InlineData("output >> \"texto\"; sa|")]
    [InlineData("sample : compose f(sa|")]
    public void Context_SpeaksAgainAfterACommentOrStringCloses(string textWithCaret)
    {
        Assert.False(ContextOf(textWithCaret).IsSilent);
    }

    [Fact]
    public void SuggestionsFor_PutsPrefixMatchesBeforeContainsMatches()
    {
        var labels = LabelsFor("se|");

        Assert.Equal(["session", "sequence"], labels[..2]);
        Assert.All(labels[2..], label => Assert.False(label.StartsWith("se", System.StringComparison.Ordinal)));
        Assert.Contains("phrase", labels);
        Assert.DoesNotContain("loop", labels);
    }

    [Fact]
    public void SuggestionsFor_WithinTheSameMatch_ListsKeywordsBeforeTypes()
    {
        var kinds = Engine.SuggestionsFor(ContextOf("se|")).Take(2).Select(suggestion => suggestion.Kind);

        Assert.Equal([CompletionKind.Keyword, CompletionKind.Snippet], kinds);
    }

    [Fact]
    public void SuggestionsFor_OffersKeywordAndSnippetForTheSameWord()
    {
        var kinds = Engine.SuggestionsFor(ContextOf("loo|")).Where(suggestion => suggestion.Label == "loop").Select(suggestion => suggestion.Kind);

        Assert.Equal([CompletionKind.Keyword, CompletionKind.Snippet], kinds);
    }

    [Fact]
    public void SuggestionsFor_WithoutPrefix_OffersTheWholeVocabulary()
    {
        Assert.Contains("session", LabelsFor("|"));
        Assert.Equal(30, Engine.SuggestionsFor(ContextOf("|")).Count(suggestion => suggestion.Kind != CompletionKind.Snippet));
    }

    [Fact]
    public void Keywords_CarryTheReferenceEquivalent()
    {
        var repeat = Engine.SuggestionsFor(ContextOf("rep|")).First(suggestion => suggestion.Kind == CompletionKind.Keyword);

        Assert.Equal("while", repeat.Detail);
        Assert.NotEmpty(repeat.Description);
    }

    [Fact]
    public void Snippet_ExpandsWithTheLineIndentationAndPlacesTheCaret()
    {
        var snippet = Engine.SuggestionsFor(ContextOf("rep|")).First(suggestion => suggestion.Kind == CompletionKind.Snippet);

        var insertion = snippet.ExpandFor("    ");

        Assert.Equal("repeat [] >\n        \n    <", insertion.Text);
        Assert.Equal("repeat [".Length, insertion.CaretIndex);
    }

    [Fact]
    public void Expand_WithoutMarker_LeavesTheCaretAtTheEnd()
    {
        var insertion = new CompletionSuggestion("edad", CompletionKind.Variable, "edad").ExpandFor("  ");

        Assert.Equal(new CompletionInsertion("edad", 4), insertion);
    }
}
