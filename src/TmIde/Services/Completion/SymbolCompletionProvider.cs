using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Tempo.Language.Outline;
using Tempo.Language.Syntax;

using TmIde.Controls.Syntax;

namespace TmIde.Services.Completion;

/// <summary>
/// Sugiere los nombres del usuario. Tiene dos fuentes:
/// <list type="number">
///   <item>El resumen del compilador (<see cref="FileOutline"/>): funciones
///   visibles con su firma y nombres locales visibles en la línea del cursor,
///   con su tipo.</item>
///   <item>Las palabras ya escritas en el documento que el resumen aún no
///   conoce, porque se acaban de escribir y el archivo todavía no compila.</item>
/// </list>
/// Así la lista sirve tanto con código válido como a medio escribir.
/// </summary>
public sealed class SymbolCompletionProvider : ICompletionProvider
{
    private static readonly Dictionary<LocalKind, CompletionKind> KindByLocal = new()
    {
        [LocalKind.Variable] = CompletionKind.Variable,
        [LocalKind.Constant] = CompletionKind.Constant,
        [LocalKind.Parameter] = CompletionKind.Parameter,
    };

    private readonly Func<FileOutline> _outline;

    /// <param name="outline">Resumen vigente del documento activo; se consulta en cada petición.</param>
    public SymbolCompletionProvider(Func<FileOutline> outline)
    {
        _outline = outline;
    }

    public IEnumerable<CompletionSuggestion> SuggestionsFor(CompletionContext context)
    {
        var outline = _outline() ?? FileOutline.Empty;

        var declared = outline.LocalsVisibleAt(context.Line)
            .Select(ToSuggestion)
            .Concat(outline.Functions.Select(ToSuggestion))
            .ToList();

        var known = declared.Select(suggestion => suggestion.Label).ToHashSet(StringComparer.Ordinal);

        return declared.Concat(WordsWrittenIn(context).Where(known.Add).Select(ToWordSuggestion));
    }

    private static CompletionSuggestion ToSuggestion(LocalOutline local) =>
        new(local.Name, KindByLocal[local.Kind], local.Name, local.TypeName, $"{local.TypeName} {local.Name} (línea {local.Line})");

    // Al aceptar una función se escriben los paréntesis y el cursor queda dentro.
    private static CompletionSuggestion ToSuggestion(FunctionOutline function) =>
        new(function.Name, CompletionKind.Function, $"{function.Name}({CompletionSuggestion.CaretMarker})",
            function.ReturnType, $"{function.Signature}\n{function.FileName}, línea {function.Line}");

    private static CompletionSuggestion ToWordSuggestion(string word) =>
        new(word, CompletionKind.Word, word, "texto", "Nombre escrito en este documento.");

    // Identificador completo de TM (§2): empieza por letra y no continúa a otro.
    private static readonly Regex Identifier = new(@"(?<![A-Za-z0-9_])[A-Za-z][A-Za-z0-9_]*", RegexOptions.CultureInvariant);

    // Identificadores del documento fuera de comentarios y cadenas, sin las
    // palabras reservadas ni la palabra que se está escribiendo.
    private static IEnumerable<string> WordsWrittenIn(CompletionContext context)
    {
        var commentsAndStrings = TmSyntaxScanner.Scan(context.Text)
            .Where(span => span.Kind is TmSyntaxKind.Comment or TmSyntaxKind.StringLiteral)
            .ToList();

        return Identifier.Matches(context.Text)
            .Where(word => word.Index != context.PrefixStart)
            .Where(word => !commentsAndStrings.Any(span => span.Contains(word.Index)))
            .Select(word => word.Value)
            .Where(word => !SyntaxFacts.TryGetReservedWord(word, out _));
    }
}
