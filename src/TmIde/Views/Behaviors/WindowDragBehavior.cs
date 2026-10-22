using System.Windows;
using System.Windows.Input;

using Microsoft.Xaml.Behaviors;

namespace TmIde.Views.Behaviors;

/// <summary>
/// Convierte un elemento en barra de título de su ventana (las ventanas usan
/// chrome propio, WindowStyle=None): arrastrarlo mueve la ventana y, con
/// <see cref="MaximizeOnDoubleClick"/>, el doble clic maximiza o restaura.
/// </summary>

public sealed class WindowDragBehavior : Behavior<FrameworkElement>
{
    public static readonly DependencyProperty MaximizeOnDoubleClickProperty = DependencyProperty.Register(
        nameof(MaximizeOnDoubleClick), typeof(bool), typeof(WindowDragBehavior), new PropertyMetadata(false));

    public bool MaximizeOnDoubleClick
    {
        get => (bool)GetValue(MaximizeOnDoubleClickProperty);
        set => SetValue(MaximizeOnDoubleClickProperty, value);
    }

    protected override void OnAttached() => AssociatedObject.MouseLeftButtonDown += OnMouseLeftButtonDown;

    protected override void OnDetaching() => AssociatedObject.MouseLeftButtonDown -= OnMouseLeftButtonDown;

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Window.GetWindow(AssociatedObject) is not { } window)
        {
            return;
        }

        if (e.ClickCount == 2 && MaximizeOnDoubleClick)
        {
            window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            return;
        }

        window.DragMove();
    }
}
