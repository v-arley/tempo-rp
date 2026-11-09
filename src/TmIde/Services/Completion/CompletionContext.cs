using System;
using System.Linq;
using System.Text.RegularExpressions;

using Tempo.Language.Syntax;

using TmIde.Controls.Syntax;

namespace TmIde.Services.Completion;

/// <summary>
/// Lo que el autocompletado necesita saber del punto donde está el cursor: la
/// palabra a medio escribir (<see cref="Prefix"/>), la línea y su sangría, y si
/// conviene callar (<see cref="IsSilent"/>). Se calcula con el escáner tolerante
/// del coloreado, así que funciona aunque el código aún no compile.
/// </summary>
public sealed record CompletionContext
{
    private CompletionContext(string text, int caretOffset, int prefixStart, int line, string indentation, bool isSilent)
    {
        Text = text;
        CaretOffset = caretOffset;
        PrefixStart = prefixStart;
        Line = line;
        Indentation = indentation;
        IsSilent = isSilent;
    }

    /// <summary>Texto completo del documento.</summary>
    public string Text { get; }

    public int CaretOffset { get; }

    /// <summary>Posición donde empieza la palabra a medio escribir; igual a <see cref="CaretOffset"/> si no hay ninguna.</summary>
    public int PrefixStart { get; }

    /// <summary>Palabra a medio escribir justo antes del cursor.</summary>
    public string Prefix => Text[PrefixStart..CaretOffset];

    /// <summary>Línea del cursor, desde 1.</summary>
    public int Line { get; }

    /// <summary>Espacios al inicio de la línea del cursor.</summary>
    public string Indentation { get; }

    /// <summary>
    /// Verdadero donde una sugerencia estorba: dentro de un comentario o una
    /// cadena, en mitad de un número o donde se escribe un nombre nuevo
    /// (después de un tipo o de <c>compose</c>).
    /// </summary>
    public bool IsSilent { get; }

    public static CompletionContext At(string text, int caretOffset)
    {
        var caret = Math.Clamp(caretOffset, 0, text.Length);
        var prefixStart = StartOfWordBefore(text, caret);
        var lineStart = caret == 0 ? 0 : text.LastIndexOf('\n', caret - 1) + 1;
        var line = text.AsSpan(0, caret).Count('\n') + 1;
        var indentation = LeadingWhitespace(text, lineStart);

        var isSilent = IsInsideCommentOrString(text, caret)
            || IsPrefixNotAName(text, prefixStart, caret)
            || ExpectsNewName(text[lineStart..prefixStart]);

        return new CompletionContext(text, caret, prefixStart, line, indentation, isSilent);
    }

    private static int StartOfWordBefore(string text, int caret)
    {
        var start = caret;
        while (start > 0 && SyntaxFacts.IsIdentifierPart(text[start - 1]))
        {
            start--;
        }

        return start;
    }

    private static string LeadingWhitespace(string text, int lineStart)
    {
        var end = lineStart;
        while (end < text.Length && text[end] is ' ' or '\t')
        {
            end++;
        }

        return text[lineStart..end];
    }

    // Lo escrito empieza por dígito o '_': es un número, no un nombre.
    private static bool IsPrefixNotAName(string text, int prefixStart, int caret) =>
        prefixStart < caret && !SyntaxFacts.IsIdentifierStart(text[prefixStart]);

    // El cursor está dentro de un comentario o una cadena si cae en su interior,
    // o justo al final de uno sin cerrar (una línea comentada, una cadena sin
    // comilla de cierre).
    private static bool IsInsideCommentOrString(string text, int caret) =>
        TmSyntaxScanner.Scan(text)
            .Where(span => span.Kind is TmSyntaxKind.Comment or TmSyntaxKind.StringLiteral)
            .Any(span => (caret > span.Start && caret < span.End) || (caret == span.End && IsUnterminated(text, span)));

    private static bool IsUnterminated(string text, TmSyntaxSpan span)
    {
        var fragment = text.AsSpan(span.Start, span.Length);

        return span.Kind == TmSyntaxKind.StringLiteral
            ? IsUnterminatedString(fragment)
            : IsUnterminatedComment(fragment);
    }

    private static bool IsUnterminatedString(ReadOnlySpan<char> literal) =>
        literal.Length == 1 || literal[^1] != literal[0];

    // Un comentario de línea llega hasta el fin de línea; uno de bloque necesita "##" al final.
    private static bool IsUnterminatedComment(ReadOnlySpan<char> comment)
    {
        var isBlock = comment.StartsWith(BlockCommentDelimiter, StringComparison.Ordinal);

        return !isBlock
            || comment.Length < 2 * BlockCommentDelimiter.Length
            || !comment.EndsWith(BlockCommentDelimiter, StringComparison.Ordinal);
    }

    private const string BlockCommentDelimiter = "##";

    // Lo escrito antes de la palabra, en la misma línea, termina en un tipo
    // ("sample ", "sequence<wave> ") o en "compose ": lo que sigue es un nombre
    // nuevo. Los tipos salen de SyntaxFacts.
    private static readonly Regex NewNamePosition = new(
        $@"(?<!\w)({WordsWithRole(ReservedWordRole.PrimitiveType)}|compose|({WordsWithRole(ReservedWordRole.CompositeType)})\s*<[^<>]*>)\s+$",
        RegexOptions.CultureInvariant);

    private static bool ExpectsNewName(string lineBeforePrefix) => NewNamePosition.IsMatch(lineBeforePrefix);

    private static string WordsWithRole(ReservedWordRole role) =>
        string.Join('|', SyntaxFacts.ReservedWords.Where(word => word.Role == role).Select(word => word.Lexeme));
}
