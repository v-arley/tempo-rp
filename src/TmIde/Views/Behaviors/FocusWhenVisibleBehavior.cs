using System.Windows;
using System.Windows.Threading;

using Microsoft.Xaml.Behaviors;

namespace TmIde.Views.Behaviors;

/// <summary>
/// Da el foco al elemento cada vez que se vuelve visible (p. ej. el nombre del
/// formulario de nuevo proyecto). Se difiere porque el cambio de visibilidad
/// llega antes de que el elemento sea enfocable.
/// </summary>

public sealed class FocusWhenVisibleBehavior : Behavior<UIElement>
{
    protected override void OnAttached() => AssociatedObject.IsVisibleChanged += OnIsVisibleChanged;

    protected override void OnDetaching() => AssociatedObject.IsVisibleChanged -= OnIsVisibleChanged;

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            AssociatedObject.Dispatcher.BeginInvoke(DispatcherPriority.Input, () => AssociatedObject.Focus());
        }
    }
}
