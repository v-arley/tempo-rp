using System;
using System.Windows;
using System.Windows.Input;

namespace TmIde.Views;

/// <summary>
/// Ventana principal con chrome propio (WindowStyle=None + WindowChrome). El
/// arrastre y el doble clic para maximizar/restaurar se manejan aquí; los
/// botones min/max/close viven en <c>TitleMenuBarView</c>.
/// </summary>

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        StateChanged += OnStateChanged;
    }

    private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            return;
        }

        DragMove();
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        // Compensa el borde de redimensionado de WindowChrome al maximizar,
        // para que el contenido no quede recortado bajo el borde de la pantalla.
        RootLayout.Margin = WindowState == WindowState.Maximized ? new Thickness(7) : new Thickness(0);
    }

    private void TitleMenuBarView_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private void EditorHostView_Loaded(object sender, RoutedEventArgs e)
    {

    }
}
