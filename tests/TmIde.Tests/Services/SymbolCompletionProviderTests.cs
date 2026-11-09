using System;
using System.IO;
using System.Linq;

using Tempo.Language;
using Tempo.Language.Outline;

using TmIde.Services.Completion;

using Xunit;

namespace TmIde.Tests.Services;

/// <summary>
/// Autocompletado con los nombres del usuario: los del resumen del compilador
/// y, mientras el archivo no compila, las palabras ya escritas.
/// </summary>

public sealed class SymbolCompletionProviderTests
{
    private const string Caret = "|";

    private const string Program =
        "sample : compose sumar(sample a, sample b) >\n" +
        "    ret a + b;\n" +
        "<\n" +
        "session >\n" +
        "    sample edad = 20;\n" +
        "    output >> sumar(edad, 1);\n" +
        "<\n";

    // Resumen real del compilador para Program, compilado como documento abierto.
    private static FileOutline OutlineOfProgram()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tm-outline-{Guid.NewGuid():N}", "principal.tmpr");
        var compilation = Compilation.FromPath(path, new System.Collections.Generic.Dictionary<string, string> { [path] = Program });

        return compilation.OutlineOf(path) ?? throw new InvalidOperationException("El programa de prueba debe compilar.");
    }

    private static CompletionSuggestion[] SuggestionsFor(string textWithCaret, FileOutline outline)
    {
        var context = CompletionContext.At(textWithCaret.Replace(Caret, string.Empty, StringComparison.Ordinal),textWithCaret.IndexOf(Caret, StringComparison.Ordinal));

        return CompletionEngine.WithSymbols(() => outline).SuggestionsFor(context).ToArray();
    }

    [Fact]
    public void SuggestionsFor_OffersVisibleLocalsWithTheirType()
    {
        var suggestions = SuggestionsFor(Program.Replace("    output", "    ed|\n    output", StringComparison.Ordinal), OutlineOfProgram());

        var edad = suggestions.First();
        Assert.Equal(("edad", CompletionKind.Variable, "sample"), (edad.Label, edad.Kind, edad.Detail));
    }

    [Fact]
    public void SuggestionsFor_OffersFunctionsThatOpenTheirParentheses()
    {
        var sumar = SuggestionsFor("session >\n    su|\n<\n", OutlineOfProgram()).First(suggestion => suggestion.Kind == CompletionKind.Function);

        Assert.Equal("sumar(", sumar.ExpandFor(string.Empty).Text[..6]);
        Assert.Equal(6, sumar.ExpandFor(string.Empty).CaretIndex);
        Assert.StartsWith("sample : sumar(sample a, sample b)", sumar.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void SuggestionsFor_ParametersAreOnlyVisibleInsideTheirFunction()
    {
        var outline = OutlineOfProgram();

        Assert.Contains(SuggestionsFor("sample : compose sumar(sample a, sample b) >\n    ret |", outline), suggestion => suggestion.Label == "a" && suggestion.Kind == CompletionKind.Parameter);
        Assert.DoesNotContain(SuggestionsFor(Program.Replace("    output", "    |\n    output", StringComparison.Ordinal), outline), suggestion => suggestion.Kind == CompletionKind.Parameter);
    }

    [Fact]
    public void SuggestionsFor_WithoutOutline_FallsBackToWordsWrittenInTheDocument()
    {
        var suggestions = SuggestionsFor("session >\n    sample contador = 0;\n    cont|", FileOutline.Empty);

        var word = Assert.Single(suggestions, suggestion => suggestion.Label == "contador");
        Assert.Equal(CompletionKind.Word, word.Kind);
    }

    [Fact]
    public void SuggestionsFor_WordsSkipCommentsStringsAndTheWordBeingTyped()
    {
        var labels = SuggestionsFor("# comentario\noutput >> \"cadena\";\ncom|", FileOutline.Empty).Select(suggestion => suggestion.Label);

        Assert.DoesNotContain("comentario", labels);
        Assert.DoesNotContain("cadena", labels);
        Assert.DoesNotContain("com", labels);
        Assert.Contains("compose", labels);
    }

    [Fact]
    public void SuggestionsFor_DoesNotRepeatAWordTheOutlineAlreadyKnows()
    {
        var labels = SuggestionsFor(Program.Replace("    output", "    ed|\n    output", StringComparison.Ordinal), OutlineOfProgram())
            .Where(suggestion => suggestion.Label == "edad");

        Assert.Single(labels);
    }
}
