using System;
using System.Windows.Media;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace TmIde.Controls;

/// Colorea los mensajes del panel Output sin modificar
/// su contenido ni sus posiciones de navegación.
public sealed class OutputLogColorizer : DocumentColorizingTransformer
{
    private readonly Func<bool> _isEnabled;
    private readonly Func<TextDocument?> _documentProvider;
    private readonly Func<string, Brush?> _brushResolver;

    public OutputLogColorizer(
        Func<bool> isEnabled,
        Func<TextDocument?> documentProvider,
        Func<string, Brush?> brushResolver)
    {
        _isEnabled = isEnabled;
        _documentProvider = documentProvider;
        _brushResolver = brushResolver;
    }

    /// Determina el recurso de color para una línea.
    /// Los nombres son claves que definen todos los temas (Theme.*.xaml).
    public static string? GetBrushKeyForLine(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var value = text.Trim();

        // Encabezados de categorías
        switch (value)
        {
            case "── IMPORTACIÓN ──":
                return "SyntaxNumberBrush";

            case "── ANÁLISIS LÉXICO ──":
                return "SyntaxLiteralBrush";

            case "── ANÁLISIS SINTÁCTICO ──":
                return "SyntaxStringBrush";

            case "── ANÁLISIS SEMÁNTICO ──":
                return "SyntaxTypeBrush";

            case "── EJECUCIÓN ──":
                return "DiagnosticErrorBrush";

            case "── ADVERTENCIAS ──":
                return "DiagnosticWarningBrush";

            case "── OTROS DIAGNÓSTICOS ──":
                return "SecondaryForegroundBrush";
        }

        // Encabezados individuales de diagnósticos
        if (value.StartsWith(
            "[ERROR ",
            StringComparison.Ordinal))
        {
            return "DiagnosticErrorBrush";
        }

        if (value.StartsWith(
            "[ADVERTENCIA ",
            StringComparison.Ordinal))
        {
            return "DiagnosticWarningBrush";
        }

        // Mensajes del resultado de compilación
        if (value.StartsWith(
            "COMPILE → Compilación exitosa",
            StringComparison.OrdinalIgnoreCase))
        {
            return "SyntaxNumberBrush";
        }

        if (value.StartsWith(
            "COMPILE → Compilación con ",
            StringComparison.OrdinalIgnoreCase))
        {
            return "DiagnosticErrorBrush";
        }

        return null;
    }

    /// AvalonEdit llama este método cuando necesita
    /// dibujar una línea visible del log.
    protected override void ColorizeLine(DocumentLine line)
    {
        // No colorear si el control es la pestaña Run.
        if (!_isEnabled() || line.Length == 0)
        {
            return;
        }

        var document = _documentProvider();

        if (document is null)
        {
            return;
        }

        var text = document.GetText(
            line.Offset,
            line.Length);

        // Revisar si toda la línea tiene un color
        var brushKey = GetBrushKeyForLine(text);

        if (brushKey is not null)
        {
            var brush = _brushResolver(brushKey);

            if (brush is not null)
            {
                Paint(
                    line.Offset,
                    line.EndOffset,
                    brush);
            }

            return;
        }

        // Los campos del diagnóstico conservan
        // su contenido, pero destacan sus etiquetas
        if (text.StartsWith("Archivo:", StringComparison.Ordinal) ||
            text.StartsWith("Línea:", StringComparison.Ordinal) ||
            text.StartsWith("Descripción:", StringComparison.Ordinal) ||
            text.StartsWith("Posible solución:", StringComparison.Ordinal))
        {
            var separatorIndex = text.IndexOf(':');

            if (separatorIndex < 0)
            {
                return;
            }

            var labelBrush =
                _brushResolver("PrimaryForegroundBrush");

            if (labelBrush is null)
            {
                return;
            }

            Paint(
                line.Offset,
                line.Offset + separatorIndex + 1,
                labelBrush);
        }
    }

    /// Aplica el color únicamente al tramo indicado.
    /// No altera el texto ni agrega elementos.
    private void Paint(
        int start,
        int end,
        Brush brush)
    {
        if (end <= start)
        {
            return;
        }

        ChangeLinePart(
            start,
            end,
            element =>
                element.TextRunProperties.SetForegroundBrush(
                    brush));
    }
}
