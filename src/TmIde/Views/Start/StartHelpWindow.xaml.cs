using System.Windows;
using System.Windows.Input;

namespace TmIde.Views.Start;

/// <summary>
/// Guía modal que resume la pantalla de inicio. Contenido estático, con el
/// mismo chrome propio (WindowStyle=None) que <see cref="StartWindow"/>.
/// </summary>

public partial class StartHelpWindow : Window
{
    public StartHelpWindow() => InitializeComponent();

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }
}
