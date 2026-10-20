using System;
using System.Collections.Generic;
using System.Linq;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

namespace TmIde.Controls.Syntax;

/// <summary>
/// Calcula los plegados del código TM: los bloques <c>&gt;</c> … <c>&lt;</c>
/// (según <see cref="TmBlockDelimiters"/>) y los comentarios de bloque
/// <c>##…##</c> de varias líneas. Los textos dentro de cadenas y comentarios se
/// ignoran, reutilizando <see cref="TmSyntaxScanner"/>.
/// </summary>

public static class TmFoldingStrategy
{
    private const string BlockCommentDelimiter = "##";
    private const string FoldedPlaceholder = " … ";

    public static void UpdateFoldings(FoldingManager manager, TextDocument document)
    {
        manager.UpdateFoldings(CreateFoldings(document.Text), firstErrorOffset: -1);
    }

    public static IEnumerable<NewFolding> CreateFoldings(string text)
    {
        var spans = TmSyntaxScanner.Scan(text);
        var code = MaskCommentsAndStrings(text, spans);

        return BlockCommentFoldings(text, spans)
            .Concat(CodeBlockFoldings(text, code))
            .OrderBy(folding => folding.StartOffset)
            .ThenBy(folding => folding.EndOffset)
            .ToList();
    }

    // Comentarios de bloque de varias líneas: se pliega el interior y quedan
    // visibles los "##" de apertura y cierre.
    private static IEnumerable<NewFolding> BlockCommentFoldings(string text, IReadOnlyList<TmSyntaxSpan> spans)
    {
        foreach (var span in spans.Where(span => span.Kind == TmSyntaxKind.Comment))
        {
            var comment = text.AsSpan(span.Start, span.Length);
            if (!comment.StartsWith(BlockCommentDelimiter, StringComparison.Ordinal))
            {
                continue;
            }

            var isClosed = comment.Length >= 2 * BlockCommentDelimiter.Length
                && comment.EndsWith(BlockCommentDelimiter, StringComparison.Ordinal);
            var start = span.Start + BlockCommentDelimiter.Length;
            var end = isClosed ? span.End - BlockCommentDelimiter.Length : span.End;

            if (CreateIfMultiline(text, start, end) is { } folding)
            {
                yield return folding;
            }
        }
    }

    // Bloques "> … <": empareja cada línea que abre con la siguiente que cierra
    // al mismo nivel. Recorre el código ya enmascarado para no contar los
    // símbolos que aparecen dentro de cadenas o comentarios.
    private static IEnumerable<NewFolding> CodeBlockFoldings(string text, string code)
    {
        var openings = new Stack<int>();
        var lineStart = 0;
        while (lineStart <= code.Length)
        {
            var lineEnd = code.IndexOf('\n', lineStart);
            lineEnd = lineEnd < 0 ? code.Length : lineEnd;

            var line = code.AsSpan(lineStart, lineEnd - lineStart);
            if (TmBlockDelimiters.ClosesBlock(line))
            {
                var closeOffset = lineStart + (line.Length - line.TrimStart().Length);
                if (openings.Count > 0 && CreateIfMultiline(text, openings.Pop() + 1, closeOffset) is { } folding)
                {
                    yield return folding;
                }
            }
            else if (TmBlockDelimiters.OpensBlock(line))
            {
                var openOffset = lineStart + line.TrimEnd().Length - 1;
                openings.Push(openOffset);
            }

            lineStart = lineEnd + 1;
        }
    }

    // Sustituye por espacios el contenido de comentarios y cadenas (conservando
    // los saltos de línea), para que los offsets coincidan con el texto original.
    private static string MaskCommentsAndStrings(string text, IReadOnlyList<TmSyntaxSpan> spans)
    {
        var code = text.ToCharArray();
        foreach (var span in spans.Where(span => span.Kind is TmSyntaxKind.Comment or TmSyntaxKind.StringLiteral))
        {
            for (var position = span.Start; position < span.End; position++)
            {
                if (code[position] != '\n')
                {
                    code[position] = ' ';
                }
            }
        }

        return new string(code);
    }

    private static NewFolding? CreateIfMultiline(string text, int start, int end)
    {
        var spansSeveralLines = end > start && text.IndexOf('\n', start, end - start) >= 0;
        return spansSeveralLines ? new NewFolding(start, end) { Name = FoldedPlaceholder } : null;
    }
}
