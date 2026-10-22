using System;
using System.Windows;

namespace TmIde.Views;

/// <summary>
/// Ventana principal con chrome propio (WindowStyle=None + WindowChrome). El
/// arrastre y el doble clic para maximizar/restaurar los da
/// <c>WindowDragBehavior</c> en la barra de título; los botones min/max/close
/// viven en <c>TitleMenuBarView</c>.
/// </summary>

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        StateChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        // Compensa el borde de redimensionado de WindowChrome al maximizar,
        // para que el contenido no quede recortado bajo el borde de la pantalla.
        RootLayout.Margin = WindowState == WindowState.Maximized ? new Thickness(7) : new Thickness(0);
    }
}
