using System.Windows;

using TmIde.Views.Behaviors;

namespace TmIde.Views;

/// <summary>
/// Ventana principal con chrome propio (WindowStyle=None + WindowChrome). El
/// arrastre y el doble clic para maximizar/restaurar los da
/// <c>WindowDragBehavior</c> en la barra de título y <c>MaximizeToWorkArea</c> mantiene la ventana
/// maximizada dentro del área útil (sin tapar la barra de tareas); los botones min/max/close
/// viven en <c>TitleMenuBarView</c>.
/// </summary>

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        MaximizeToWorkArea.Attach(this);
    }
}
