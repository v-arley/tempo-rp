using System.Windows.Controls;
using TmIde.Controls;
using TmIde.ViewModels.Panel;

namespace TmIde.Views.Panel;

public partial class BottomPanelView : UserControl
{
    public BottomPanelView()
    {
        InitializeComponent();
    }

    /// Envía al ViewModel de Output la línea sobre la que se hizo doble clic
    private void OnOutputLineDoubleClicked(
        object sender,
        LogLineDoubleClickedEventArgs e)
    {
        if (sender is not LogView logView)
        {
            return;
        }

        if (logView.DataContext is not OutputPaneViewModel output)
        {
            return;
        }

        output.ActivateLine(e.LineNumber);
    }

    private void TabControl_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
    }
}
