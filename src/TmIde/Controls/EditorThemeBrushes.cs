using System.Windows.Media;

using ICSharpCode.AvalonEdit.Editing;

namespace TmIde.Controls;

/// <summary>
/// Pinceles de AvalonEdit que no son propiedades de dependencia (selección y
/// cursor). Los controles los reciben del tema por <c>DynamicResource</c> y los
/// aplican con estos métodos. Compartido por el editor de código y los paneles de log.
/// </summary>

internal static class EditorThemeBrushes
{
    private const double SelectionOpacity = 0.45;

    /// <summary>Selección: el acento con transparencia para que el texto siga legible.</summary>
    public static void ApplySelection(TextArea textArea, Brush accent)
    {
        textArea.SelectionBrush = WithOpacity(accent, SelectionOpacity);
        textArea.SelectionBorder = null;
        textArea.SelectionForeground = null;
    }

    public static Brush WithOpacity(Brush brush, double opacity)
    {
        var copy = brush.CloneCurrentValue();
        copy.Opacity = opacity;
        return copy;
    }
}
