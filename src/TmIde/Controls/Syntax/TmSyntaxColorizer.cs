using System;
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

    
    /// Obtiene la palabra reservada situada en un desplazamiento del documento
    /// Reutiliza los tokens del coloreado de sintaxis
    /// Ignora comentarios, cadenas y nombres similares
    public string? GetReservedWordAt(
        TextDocument document,
        int offset)
    {
        if (offset < 0 || offset >= document.TextLength)
        {
            return null;
        }

        foreach (var span in _spans)
        {
            // Los fragmentos ya están ordenados.
            if (span.Start > offset)
            {
                break;
            }

            if (!span.Contains(offset))
            {
                continue;
            }

            if (span.Kind is
                TmSyntaxKind.Keyword
                or TmSyntaxKind.Type
                or TmSyntaxKind.BuiltInValue)
            {
                return document.GetText(
                    span.Start,
                    span.Length);
            }

            return null;
        }

        return null;
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

            if (span.End <= lineStart || BrushFor(span.Kind) is not { } brush)
            {
                continue;
            }

            ChangeLinePart(
                Math.Max(span.Start, lineStart),
                Math.Min(span.End, lineEnd),
                element => element.TextRunProperties.SetForegroundBrush(brush));
        }
    }

    private static Brush? BrushFor(TmSyntaxKind kind)
    {
        var key = kind switch
        {
            TmSyntaxKind.Keyword => "SyntaxKeywordBrush",
            TmSyntaxKind.Type => "SyntaxTypeBrush",
            TmSyntaxKind.BuiltInValue => "SyntaxLiteralBrush",
            TmSyntaxKind.Number => "SyntaxNumberBrush",
            TmSyntaxKind.StringLiteral => "SyntaxStringBrush",
            _ => "SyntaxCommentBrush",
        };

        return Application.Current?.TryFindResource(key) as Brush;
    }
}
