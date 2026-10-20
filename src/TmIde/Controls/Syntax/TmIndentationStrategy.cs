using System;
using System.Collections.Generic;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Indentation;

namespace TmIde.Controls.Syntax;

/// <summary>
/// Sangría automática para TM. Al pulsar Enter la línea nueva hereda la sangría
/// de la última línea no vacía, un nivel más si esa línea abre un bloque. Una
/// línea que empieza cerrando un bloque baja un nivel. Las reglas de apertura y
/// cierre son las de <see cref="TmBlockDelimiters"/>.
/// </summary>

public sealed class TmIndentationStrategy : DefaultIndentationStrategy
{
    public const int IndentSize = 4;

    private static readonly string IndentUnit = new(' ', IndentSize);

    public override void IndentLine(TextDocument document, DocumentLine line)
    {
        var previous = PreviousNonBlankLine(document, line);
        if (previous is null)
        {
            return;
        }

        var spans = TmSyntaxScanner.Scan(document.Text);
        var previousOpensBlock = EndsWithBlockOpen(document, previous, spans);
        var lineClosesBlock = StartsWithBlockClose(document, line);

        var indent = LeadingWhitespace(document, previous);
        if (previousOpensBlock && !lineClosesBlock)
        {
            indent += IndentUnit;
        }
        else if (!previousOpensBlock && lineClosesBlock)
        {
            indent = indent.Length >= IndentSize ? indent[..^IndentSize] : string.Empty;
        }

        var currentIndent = TextUtilities.GetWhitespaceAfter(document, line.Offset);
        if (document.GetText(currentIndent) != indent)
        {
            document.Replace(currentIndent, indent);
        }
    }

    public static bool StartsWithBlockClose(TextDocument document, DocumentLine line) =>
        TmBlockDelimiters.ClosesBlock(document.GetText(line));

    private static DocumentLine? PreviousNonBlankLine(TextDocument document, DocumentLine line)
    {
        var previous = line.PreviousLine;
        while (previous is not null && string.IsNullOrWhiteSpace(document.GetText(previous)))
        {
            previous = previous.PreviousLine;
        }

        return previous;
    }

    private static string LeadingWhitespace(TextDocument document, DocumentLine line) =>
        document.GetText(TextUtilities.GetWhitespaceAfter(document, line.Offset));

    // Busca el último código de la línea saltando espacios y comentarios finales.
    // Si lo último es una cadena o un carácter, la línea no abre bloque.
    private static bool EndsWithBlockOpen(TextDocument document, DocumentLine line, IReadOnlyList<TmSyntaxSpan> spans)
    {
        var codeEnd = line.EndOffset;
        while (codeEnd > line.Offset)
        {
            var lastPosition = codeEnd - 1;
            if (char.IsWhiteSpace(document.GetCharAt(lastPosition)))
            {
                codeEnd = lastPosition;
                continue;
            }

            if (FindSpanAt(spans, lastPosition) is not { } span)
            {
                break;
            }

            if (span.Kind != TmSyntaxKind.Comment)
            {
                return false;
            }

            // Un comentario de bloque puede empezar en una línea anterior.
            codeEnd = Math.Max(span.Start, line.Offset);
        }

        return TmBlockDelimiters.OpensBlock(document.GetText(line.Offset, codeEnd - line.Offset));
    }

    private static TmSyntaxSpan? FindSpanAt(IReadOnlyList<TmSyntaxSpan> spans, int position)
    {
        foreach (var span in spans)
        {
            if (span.Contains(position))
            {
                return span;
            }
        }

        return null;
    }
}
