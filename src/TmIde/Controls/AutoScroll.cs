using System.Windows;
using System.Windows.Controls;

namespace TmIde.Controls;

/// <summary>
/// Comportamiento adjunto para <see cref="ScrollViewer"/>: cuando su contenido
/// crece en alto, desplaza automáticamente hasta el final. Uso en XAML:
/// <c>controls:AutoScroll.ToEnd="True"</c>.
/// </summary>

public static class AutoScroll
{
    public static readonly DependencyProperty ToEndProperty =
        DependencyProperty.RegisterAttached(
            "ToEnd",
            typeof(bool),
            typeof(AutoScroll),
            new PropertyMetadata(false, OnToEndChanged));

    public static bool GetToEnd(DependencyObject element) => (bool)element.GetValue(ToEndProperty);

    public static void SetToEnd(DependencyObject element, bool value) => element.SetValue(ToEndProperty, value);

    private static void OnToEndChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
    {
        if (element is not ScrollViewer scrollViewer)
        {
            return;
        }

        if (args.NewValue is true)
        {
            scrollViewer.ScrollChanged += OnScrollChanged;
        }
        else
        {
            scrollViewer.ScrollChanged -= OnScrollChanged;
        }
    }

    private static void OnScrollChanged(object sender, ScrollChangedEventArgs args)
    {
        if (args.ExtentHeightChange > 0 && sender is ScrollViewer scrollViewer)
        {
            scrollViewer.ScrollToBottom();
        }
    }
}
