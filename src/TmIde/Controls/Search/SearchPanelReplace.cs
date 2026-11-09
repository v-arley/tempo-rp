using System.Windows;
using System.Windows.Input;

namespace TmIde.Controls.Search;

/// <summary>
/// Extiende el <c>SearchPanel</c> de AvalonEdit con reemplazo sin heredar de él:
/// propiedades adjuntas que la plantilla del panel (Styles.xaml) enlaza, y
/// comandos que sus botones disparan y <see cref="FindReplaceController"/> atiende.
/// </summary>
public static class SearchPanelReplace
{
    public static readonly DependencyProperty ReplacementTextProperty = DependencyProperty.RegisterAttached(
        "ReplacementText",
        typeof(string),
        typeof(SearchPanelReplace),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty IsReplaceVisibleProperty = DependencyProperty.RegisterAttached(
        "IsReplaceVisible",
        typeof(bool),
        typeof(SearchPanelReplace),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>Resumen del último reemplazo ("3 reemplazos", "Sin coincidencias").</summary>
    public static readonly DependencyProperty StatusProperty = DependencyProperty.RegisterAttached(
        "Status",
        typeof(string),
        typeof(SearchPanelReplace),
        new FrameworkPropertyMetadata(string.Empty));

    /// <summary>Reemplaza la coincidencia seleccionada y pasa a la siguiente (Enter en el campo de reemplazo).</summary>
    public static RoutedCommand ReplaceNext { get; } = new(nameof(ReplaceNext), typeof(SearchPanelReplace));

    /// <summary>Reemplaza todas las coincidencias del documento (Ctrl+Alt+Enter).</summary>
    public static RoutedCommand ReplaceAll { get; } = new(nameof(ReplaceAll), typeof(SearchPanelReplace));

    public static string GetReplacementText(DependencyObject element) => (string)element.GetValue(ReplacementTextProperty);

    public static void SetReplacementText(DependencyObject element, string value) => element.SetValue(ReplacementTextProperty, value);

    public static bool GetIsReplaceVisible(DependencyObject element) => (bool)element.GetValue(IsReplaceVisibleProperty);

    public static void SetIsReplaceVisible(DependencyObject element, bool value) => element.SetValue(IsReplaceVisibleProperty, value);

    public static string GetStatus(DependencyObject element) => (string)element.GetValue(StatusProperty);

    public static void SetStatus(DependencyObject element, string value) => element.SetValue(StatusProperty, value);
}
