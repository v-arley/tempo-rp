using System;
using System.Collections.Generic;
using System.Linq;

using Tempo.Language.Outline;

namespace TmIde.Services.Completion;

/// <summary>
/// Reúne varias fuentes de sugerencias (Composite) y produce la lista que ve el
/// usuario: calla donde el contexto lo pide, se queda con las que contienen lo
/// escrito, quita duplicados y las ordena por lo bien que coinciden y por su
/// clase (<see cref="CompletionKind"/>).
/// </summary>
public sealed class CompletionEngine
{
    private readonly IReadOnlyList<ICompletionProvider> _providers;

    public CompletionEngine(params ICompletionProvider[] providers)
    {
        _providers = providers;
    }

    /// <summary>Vocabulario del lenguaje y plantillas, sin símbolos del usuario.</summary>
    public static CompletionEngine WithoutSymbols() =>
        new(new ReservedWordCompletionProvider(), new SnippetCompletionProvider());

    /// <summary>Vocabulario, plantillas y los nombres del usuario que describe <paramref name="outline"/>.</summary>
    public static CompletionEngine WithSymbols(Func<FileOutline> outline) =>
        new(new SymbolCompletionProvider(outline), new ReservedWordCompletionProvider(), new SnippetCompletionProvider());

    public IReadOnlyList<CompletionSuggestion> SuggestionsFor(CompletionContext context)
    {
        if (context.IsSilent)
        {
            return [];
        }

        var prefix = context.Prefix;

        return _providers
            .SelectMany(provider => provider.SuggestionsFor(context))
            .DistinctBy(suggestion => (suggestion.Label, suggestion.Kind))
            .Select(suggestion => (Suggestion: suggestion, Quality: MatchQualityOf(suggestion.Label, prefix)))
            .Where(candidate => candidate.Quality != MatchQuality.None)
            .OrderBy(candidate => candidate.Quality)
            .ThenBy(candidate => candidate.Suggestion.Kind)
            .ThenBy(candidate => candidate.Suggestion.Label, StringComparer.Ordinal)
            .Select(candidate => candidate.Suggestion)
            .ToList();
    }

    // Cuanto antes en la enumeración, mejor coincidencia.
    private enum MatchQuality
    {
        ExactPrefix,
        Prefix,
        Contains,
        None,
    }

    private static MatchQuality MatchQualityOf(string label, string prefix) =>
        label.StartsWith(prefix, StringComparison.Ordinal) ? MatchQuality.ExactPrefix
        : label.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? MatchQuality.Prefix
        : label.Contains(prefix, StringComparison.OrdinalIgnoreCase) ? MatchQuality.Contains
        : MatchQuality.None;
}
