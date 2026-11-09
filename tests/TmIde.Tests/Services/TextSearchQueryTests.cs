using System.Linq;

using TmIde.Services.Search;

using Xunit;

namespace TmIde.Tests.Services;

/// <summary>
/// Motor de búsqueda del editor: una <see cref="TextSearchQuery"/> se compila en
/// un <see cref="TextSearcher"/> que encuentra y calcula reemplazos sobre texto plano.
/// </summary>

public sealed class TextSearchQueryTests
{
    private const string Source = "sample edad = 1;\nsample Edad2 = edad + 1;";

    private static TextSearcher Compile(TextSearchQuery query)
    {
        Assert.True(query.TryCompile(out var searcher, out _));
        return searcher;
    }

    private static string[] FoundTexts(TextSearchQuery query, string text = Source) =>
        Compile(query).FindAll(text).Select(match => text.Substring(match.Offset, match.Length)).ToArray();

    [Fact]
    public void Literal_IgnoresCaseByDefault()
    {
        Assert.Equal(["edad", "Edad", "edad"], FoundTexts(new TextSearchQuery("edad")));
    }

    [Fact]
    public void MatchCase_DistinguishesCase()
    {
        Assert.Equal(["edad", "edad"], FoundTexts(new TextSearchQuery("edad", MatchCase: true)));
    }

    [Fact]
    public void WholeWord_SkipsIdentifiersThatOnlyContainThePattern()
    {
        Assert.Equal(["edad", "edad"], FoundTexts(new TextSearchQuery("edad", WholeWord: true)));
    }

    [Fact]
    public void WholeWord_TreatsUnderscoreAsPartOfTheWord()
    {
        Assert.Empty(FoundTexts(new TextSearchQuery("edad", WholeWord: true), "mi_edad = 1;"));
    }

    [Fact]
    public void Literal_EscapesRegexCharacters()
    {
        Assert.Equal(["a + 1"], FoundTexts(new TextSearchQuery("a + 1"), "x = a + 1; y = aa1;"));
    }

    [Fact]
    public void Regex_FindsByExpression()
    {
        Assert.Equal(["1", "2", "1"], FoundTexts(new TextSearchQuery(@"\d", UseRegex: true)));
    }

    [Fact]
    public void Regex_DiscardsEmptyMatches()
    {
        Assert.Empty(FoundTexts(new TextSearchQuery("^", UseRegex: true)));
    }

    [Fact]
    public void InvalidRegex_FailsToCompileWithAReason()
    {
        var compiled = new TextSearchQuery("(edad", UseRegex: true).TryCompile(out var searcher, out var error);

        Assert.False(compiled);
        Assert.NotEmpty(error);
        Assert.Same(TextSearcher.Nothing, searcher);
    }

    [Fact]
    public void EmptyQuery_FailsToCompileWithoutError()
    {
        var compiled = new TextSearchQuery(string.Empty).TryCompile(out var searcher, out var error);

        Assert.False(compiled);
        Assert.Empty(error);
        Assert.Empty(searcher.FindAll(Source));
    }

    [Fact]
    public void FindNext_WrapsToTheFirstMatch()
    {
        var searcher = Compile(new TextSearchQuery("sample"));

        Assert.Equal(17, searcher.FindNext(Source, 1)?.Offset);
        Assert.Equal(0, searcher.FindNext(Source, 18)?.Offset);
    }

    [Fact]
    public void ReplacementFor_Regex_ExpandsGroups()
    {
        const string text = "sample edad = 1;";
        var searcher = Compile(new TextSearchQuery(@"(\w+) = (\d+)", UseRegex: true));
        var match = searcher.FindAll(text).Single();

        Assert.Equal("1 = edad", searcher.ReplacementFor(text, match, "$2 = $1"));
    }

    [Fact]
    public void ReplacementFor_Literal_InsertsDollarSignsVerbatim()
    {
        var searcher = Compile(new TextSearchQuery("edad"));
        var match = searcher.FindAll(Source)[0];

        Assert.Equal("$1", searcher.ReplacementFor(Source, match, "$1"));
    }
}
