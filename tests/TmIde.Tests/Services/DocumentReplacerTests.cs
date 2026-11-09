using ICSharpCode.AvalonEdit.Document;

using TmIde.Services.Search;

using Xunit;

namespace TmIde.Tests.Services;

/// <summary>
/// Reemplazo en el documento visible: paso a paso (primero selecciona, luego
/// reemplaza) y todo de una vez, deshaciendo con una sola operación.
/// </summary>

public sealed class DocumentReplacerTests
{
    private static TextSearcher SearcherFor(TextSearchQuery query)
    {
        Assert.True(query.TryCompile(out var searcher, out _));
        return searcher;
    }

    [Fact]
    public void ReplaceNext_WhenTheSelectionIsNotAMatch_OnlyFindsTheNextOne()
    {
        var document = new TextDocument("edad = edad + 1;");

        var step = DocumentReplacer.ReplaceNext(document, SearcherFor(new TextSearchQuery("edad")), "anios", new TextMatch(2, 0));

        Assert.False(step.Replaced);
        Assert.Equal(new TextMatch(7, 4), step.Next);
        Assert.Equal("edad = edad + 1;", document.Text);
    }

    [Fact]
    public void ReplaceNext_WhenTheSelectionIsAMatch_ReplacesItAndFindsTheNextOne()
    {
        var document = new TextDocument("edad = edad + 1;");

        var step = DocumentReplacer.ReplaceNext(document, SearcherFor(new TextSearchQuery("edad")), "anios", new TextMatch(0, 4));

        Assert.True(step.Replaced);
        Assert.Equal("anios = edad + 1;", document.Text);
        Assert.Equal(new TextMatch(8, 4), step.Next);
    }

    [Fact]
    public void ReplaceNext_ReplacementContainingThePattern_DoesNotMatchItsOwnResult()
    {
        var document = new TextDocument("a a");

        var step = DocumentReplacer.ReplaceNext(document, SearcherFor(new TextSearchQuery("a", WholeWord: true)), "aa", new TextMatch(0, 1));

        Assert.Equal("aa a", document.Text);
        Assert.Equal(new TextMatch(3, 1), step.Next);
    }

    [Fact]
    public void ReplaceAll_ReplacesEveryMatchAndReturnsTheCount()
    {
        var document = new TextDocument("sample edad = 1;\nsample edadMax = edad;");

        var count = DocumentReplacer.ReplaceAll(document, SearcherFor(new TextSearchQuery("edad", WholeWord: true)), "anios");

        Assert.Equal(2, count);
        Assert.Equal("sample anios = 1;\nsample edadMax = anios;", document.Text);
    }

    [Fact]
    public void ReplaceAll_IsUndoneInASingleStep()
    {
        var document = new TextDocument("x x x");

        DocumentReplacer.ReplaceAll(document, SearcherFor(new TextSearchQuery("x")), "y");
        document.UndoStack.Undo();

        Assert.Equal("x x x", document.Text);
    }

    [Fact]
    public void ReplaceAll_WithRegexGroups_RewritesEachMatch()
    {
        var document = new TextDocument("sample a = 1;\nwave b = 2;");

        DocumentReplacer.ReplaceAll(document, SearcherFor(new TextSearchQuery(@"(\w+) (\w+) =", UseRegex: true)), "const $1 $2 =");

        Assert.Equal("const sample a = 1;\nconst wave b = 2;", document.Text);
    }

    [Fact]
    public void ReplaceAll_WithoutMatches_LeavesTheDocumentUntouched()
    {
        var document = new TextDocument("sample a = 1;");

        Assert.Equal(0, DocumentReplacer.ReplaceAll(document, SearcherFor(new TextSearchQuery("zeta")), "y"));
        Assert.False(document.UndoStack.CanUndo);
    }
}
