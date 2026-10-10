using System;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Indentation;

namespace TmIde.Controls.Syntax;

/// <summary>
/// Sangría automática para TM. Al pulsar Enter la línea nueva hereda la sangría
/// de la última línea no vacía, un nivel más si esa línea abre un bloque
/// (<c>&gt;</c> como último código). Una línea que empieza por <c>&lt;</c> cierra
/// el bloque y baja un nivel. Mismas reglas de bloque que
/// <see cref="TmFoldingStrategy"/>: <c>&gt;</c> y <c>&lt;</c> son también
/// operadores relacionales, por eso solo cuentan al final y al inicio de línea.
/// </summary>

public sealed class TmIndentationStrategy : DefaultIndentationStrategy
{
    public const int IndentSize = 4;

    private static readonly string IndentUnit = new(' ', IndentSize);

    public override void IndentLine(TextDocument document, DocumentLine line)
    {
        var previous = line.PreviousLine;
        while (previous is not null && IsBlank(document, previous))
        {
            previous = previous.PreviousLine;
        }

        if (previous is null)
        {
            return;
        }

        var text = document.Text;
        var spans = TmSyntaxScanner.Scan(text);

        var indent = LeadingWhitespace(document, previous);
        var opens = EndsWithBlockOpen(text, previous, spans);
        var closes = StartsWithBlockClose(document, line);

        if (opens && !closes)
        {
            indent += IndentUnit;
        }
        else if (!opens && closes)
        {
            indent = indent.Length >= IndentSize ? indent[..^IndentSize] : string.Empty;
        }

        var current = TextUtilities.GetWhitespaceAfter(document, line.Offset);
        if (document.GetText(current) != indent)
        {
            document.Replace(current, indent);
        }
    }

    /// <summary>Verdadero si la línea (sin espacios finales) empieza por un <c>&lt;</c> que cierra bloque.</summary>
    public static bool StartsWithBlockClose(TextDocument document, DocumentLine line)
    {
        var trimmed = document.GetText(line).TrimStart();
        return trimmed.StartsWith('<') && !trimmed.StartsWith("<<", StringComparison.Ordinal);
    }

    private static bool IsBlank(TextDocument document, DocumentLine line) =>
        string.IsNullOrWhiteSpace(document.GetText(line));

    private static string LeadingWhitespace(TextDocument document, DocumentLine line) =>
        document.GetText(TextUtilities.GetWhitespaceAfter(document, line.Offset));

    // Último código de la línea, saltando espacios y un comentario final. Si lo
    // último es una cadena o un carácter, la línea no abre bloque.
    private static bool EndsWithBlockOpen(string text, DocumentLine line, System.Collections.Generic.IReadOnlyList<TmSyntaxSpan> spans)
    {
        var position = line.EndOffset - 1;
        while (position >= line.Offset)
        {
            var span = FindSpan(spans, position);
            if (span is { } found)
            {
                if (found.Kind != TmSyntaxKind.Comment)
                {
                    return false;
                }

                position = found.Start - 1;
                continue;
            }

            if (char.IsWhiteSpace(text[position]))
            {
                position--;
                continue;
            }

            return text[position] == '>' && (position == line.Offset || text[position - 1] != '>');
        }

        return false;
    }

    private static TmSyntaxSpan? FindSpan(System.Collections.Generic.IReadOnlyList<TmSyntaxSpan> spans, int position)
    {
        foreach (var span in spans)
        {
            if (position >= span.Start && position < span.Start + span.Length)
            {
                return span;
            }
        }

        return null;
    }
}
