using System.Collections.Generic;
using System.Linq;

using Tempo.Language.Syntax;

using TmIde.Controls.Syntax;

namespace TmIde.Services.Completion;

/// <summary>
/// Sugiere las palabras reservadas de TM, tomadas de <see cref="SyntaxFacts"/>,
/// con la equivalencia y la explicación de <see cref="TempoKeywordReference"/>
/// (las mismas que muestra el tooltip con Ctrl).
/// </summary>
public sealed class ReservedWordCompletionProvider : ICompletionProvider
{
    private static readonly Dictionary<ReservedWordRole, CompletionKind> KindByRole = new()
    {
        [ReservedWordRole.Keyword] = CompletionKind.Keyword,
        [ReservedWordRole.PrimitiveType] = CompletionKind.Type,
        [ReservedWordRole.CompositeType] = CompletionKind.Type,
        [ReservedWordRole.Literal] = CompletionKind.Literal,
    };

    // El vocabulario no cambia: se construye una vez.
    private static readonly IReadOnlyList<CompletionSuggestion> Vocabulary = SyntaxFacts.ReservedWords
        .Select(ToSuggestion)
        .ToList();

    public IEnumerable<CompletionSuggestion> SuggestionsFor(CompletionContext context) => Vocabulary;

    private static CompletionSuggestion ToSuggestion(ReservedWord word)
    {
        var info = TempoKeywordReference.Find(word.Lexeme);

        return new CompletionSuggestion(
            word.Lexeme,
            KindByRole[word.Role],
            word.Lexeme,
            info?.Equivalent ?? string.Empty,
            info?.Description ?? string.Empty);
    }
}
