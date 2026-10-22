using System.Windows;

namespace TmIde.Views.Start;

/// <summary>
/// Guía modal que resume la pantalla de inicio. Contenido estático, con el
/// mismo chrome propio (WindowStyle=None) que <see cref="StartWindow"/>; se
/// arrastra por <c>WindowDragBehavior</c> y se cierra con su botón o con Esc
/// (botón <c>IsCancel</c>).
/// </summary>

public partial class StartHelpWindow : Window
{
    public StartHelpWindow() => InitializeComponent();
}
