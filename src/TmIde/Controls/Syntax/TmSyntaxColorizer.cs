using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace TmIde.Controls.Syntax;

/// <summary>
/// Colorea el código TM en AvalonEdit. Reescanea el documento completo solo
/// cuando cambia (los comentarios de bloque abarcan varias líneas) y pinta los
/// fragmentos que caen en cada línea con pinceles del tema activo, resueltos en
/// cada pintado: así el cambio claro/oscuro solo requiere un redibujado.
/// </summary>

public sealed class TmSyntaxColorizer : DocumentColorizingTransformer
{
    private IReadOnlyList<TmSyntaxSpan> _spans = [];

    public void Rescan(TextDocument document)
    {
        _spans = TmSyntaxScanner.Scan(document.Text);
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (_spans.Count == 0)
        {
            return;
        }

        var lineStart = line.Offset;
        var lineEnd = line.EndOffset;
        foreach (var span in _spans)
        {
            if (span.Start >= lineEnd)
            {
                break;
            }

            var spanEnd = span.Start + span.Length;
            if (spanEnd <= lineStart)
            {
                continue;
            }

            var brush = BrushFor(span.Kind);
            if (brush is null)
            {
                continue;
            }

            ChangeLinePart(
                System.Math.Max(span.Start, lineStart),
                System.Math.Min(spanEnd, lineEnd),
                element => element.TextRunProperties.SetForegroundBrush(brush));
        }
    }

    private static Brush? BrushFor(TmSyntaxKind kind)
    {
        var key = kind switch
        {
            TmSyntaxKind.Keyword => "SyntaxKeywordBrush",
            TmSyntaxKind.Type => "SyntaxTypeBrush",
            TmSyntaxKind.Literal => "SyntaxLiteralBrush",
            TmSyntaxKind.Number => "SyntaxNumberBrush",
            TmSyntaxKind.String => "SyntaxStringBrush",
            _ => "SyntaxCommentBrush",
        };

        return Application.Current?.TryFindResource(key) as Brush;
    }
}
