using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

using TmIde.Services.Analysis;

namespace TmIde.Controls.Diagnostics;

/// <summary>Rango de un diagnóstico dentro del documento. Al ser un
/// <see cref="TextSegment"/>, AvalonEdit lo desplaza solo mientras se escribe.</summary>

public sealed class DiagnosticSegment : TextSegment
{
    public DiagnosticSegment(EditorDiagnostic diagnostic, int start, int length)
    {
        Diagnostic = diagnostic;
        StartOffset = start;
        Length = length;
    }

    public EditorDiagnostic Diagnostic { get; }
}

/// <summary>
/// Dibuja una línea ondulada bajo cada diagnóstico (roja para errores, ámbar
/// para advertencias) con los pinceles del tema activo, resueltos en cada pintado.
/// </summary>

public sealed class DiagnosticsRenderer : IBackgroundRenderer
{
    private const double WaveStep = 3;
    private const double WaveAmplitude = 1.5;

    private readonly TextView _textView;
    private TextSegmentCollection<DiagnosticSegment>? _segments;

    public DiagnosticsRenderer(TextView textView)
    {
        _textView = textView;
    }

    public KnownLayer Layer => KnownLayer.Selection;

    /// <summary>Sustituye los diagnósticos mostrados resolviendo cada rango contra el texto actual.</summary>
    public void Update(IEnumerable<EditorDiagnostic> diagnostics, TextDocument document)
    {
        var segments = new TextSegmentCollection<DiagnosticSegment>(document);
        var text = document.Text;
        foreach (var diagnostic in diagnostics)
        {
            if (DiagnosticRangeResolver.Resolve(text, diagnostic) is { } range)
            {
                segments.Add(new DiagnosticSegment(diagnostic, range.Start, range.Length));
            }
        }

        _segments = segments;
        Repaint();
    }

    public void Clear()
    {
        _segments = null;
        Repaint();
    }

    // Redraw (y no InvalidateLayer): si las líneas visibles aún no son válidas
    // —justo tras reemplazar el texto— invalidar solo la capa pintaría una vez sin
    // nada y no volvería a hacerlo; Redraw reconstruye líneas y capas.
    private void Repaint() => _textView.Redraw();

    public IEnumerable<DiagnosticSegment> SegmentsAt(int offset) =>
        _segments?.FindSegmentsContaining(offset) ?? Enumerable.Empty<DiagnosticSegment>();

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_segments is null || !textView.VisualLinesValid || textView.VisualLines.Count == 0)
        {
            return;
        }

        var viewStart = textView.VisualLines[0].FirstDocumentLine.Offset;
        var viewEnd = textView.VisualLines[^1].LastDocumentLine.EndOffset;

        foreach (var segment in _segments.FindOverlappingSegments(viewStart, viewEnd - viewStart))
        {
            var pen = PenFor(segment.Diagnostic.Severity);
            if (pen is null)
            {
                continue;
            }

            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
            {
                drawingContext.DrawGeometry(null, pen, Squiggle(rect));
            }
        }
    }

    private static Pen? PenFor(EditorDiagnosticSeverity severity)
    {
        var key = severity == EditorDiagnosticSeverity.Warning ? "DiagnosticWarningBrush" : "DiagnosticErrorBrush";
        if (Application.Current?.TryFindResource(key) is not Brush brush)
        {
            return null;
        }

        var pen = new Pen(brush, 1);
        pen.Freeze();
        return pen;
    }

    private static Geometry Squiggle(Rect rect)
    {
        var y = rect.Bottom - WaveAmplitude - 0.5;
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(rect.Left, y), isFilled: false, isClosed: false);
            var up = false;
            for (var x = rect.Left + WaveStep; x < rect.Right + WaveStep; x += WaveStep)
            {
                context.LineTo(new Point(System.Math.Min(x, rect.Right), up ? y : y + WaveAmplitude), isStroked: true, isSmoothJoin: false);
                up = !up;
            }
        }

        geometry.Freeze();
        return geometry;
    }
}
