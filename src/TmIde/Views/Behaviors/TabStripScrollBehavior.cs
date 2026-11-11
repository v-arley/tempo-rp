using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using Microsoft.Xaml.Behaviors;

namespace TmIde.Views.Behaviors;

/// <summary>
/// Navegación de la fila de pestañas: la rueda del ratón la desplaza en
/// horizontal y cada cambio de selección trae la pestaña activa a la vista.
/// Requiere un <see cref="ScrollViewer"/> llamado <c>PART_HeaderScroller</c>
/// en la plantilla del <see cref="TabControl"/>.
/// </summary>
public sealed class TabStripScrollBehavior : Behavior<TabControl>
{
    private const string HeaderScrollerPart = "PART_HeaderScroller";

    /// <summary>Píxeles que avanza la cinta por cada muesca de la rueda.</summary>
    private const double PixelsPerNotch = 60;

    protected override void OnAttached()
    {
        AssociatedObject.PreviewMouseWheel += OnPreviewMouseWheel;
        AssociatedObject.SelectionChanged += OnSelectionChanged;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewMouseWheel -= OnPreviewMouseWheel;
        AssociatedObject.SelectionChanged -= OnSelectionChanged;
    }

    private ScrollViewer? HeaderScroller =>
        AssociatedObject.Template?.FindName(HeaderScrollerPart, AssociatedObject) as ScrollViewer;

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (HeaderScroller is not { ScrollableWidth: > 0 } scroller)
        {
            return;
        }

        // Rueda abajo desplaza hacia la derecha
        double notches = e.Delta / (double)Mouse.MouseWheelDeltaForOneLine;
        scroller.ScrollToHorizontalOffset(scroller.HorizontalOffset - (notches * PixelsPerNotch));
        e.Handled = true;
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, AssociatedObject))
        {
            return;
        }

        // Diferido hasta que el TabItem del documento nuevo exista y tenga tamaño
        AssociatedObject.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, RevealSelectedTab);
    }

    private void RevealSelectedTab()
    {
        if (AssociatedObject.SelectedItem is { } item
            && AssociatedObject.ItemContainerGenerator.ContainerFromItem(item) is FrameworkElement tab)
        {
            tab.BringIntoView();
        }
    }
}
