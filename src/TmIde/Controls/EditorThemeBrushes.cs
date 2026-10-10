using System.Windows.Media;

using ICSharpCode.AvalonEdit;

namespace TmIde.Controls;

/// <summary>
/// Pinceles de AvalonEdit que no son propiedades de dependencia (selección y
/// cursor): se leen del tema activo y hay que volver a aplicarlos cada vez que
/// el tema cambia. Compartido por el editor de código y los paneles de log.
/// </summary>

internal static class EditorThemeBrushes
{
    public static void Apply(TextEditor editor)
    {
        var textArea = editor.TextArea;
        if (editor.TryFindResource("AccentBrush") is Brush accent)
        {
            // Selección: acento con transparencia para que el texto siga legible.
            var selection = accent.CloneCurrentValue();
            selection.Opacity = 0.45;
            textArea.SelectionBrush = selection;
            textArea.SelectionBorder = null;
            textArea.SelectionForeground = null;
        }

        if (editor.TryFindResource("PrimaryForegroundBrush") is Brush caret)
        {
            textArea.Caret.CaretBrush = caret;
        }
    }
}
