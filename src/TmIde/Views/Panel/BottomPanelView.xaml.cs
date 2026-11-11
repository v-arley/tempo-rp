
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

using TmIde.Controls;
using TmIde.ViewModels.Panel;

namespace TmIde.Views.Panel;

public partial class BottomPanelView : UserControl
{
    public BottomPanelView()
    {
        InitializeComponent();
    }

    /// Devuelve el foco al campo de entrada cada vez
    /// que el programa solicita un nuevo dato.
    private void OnRunInputEnabledChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true ||
            sender is not TextBox inputBox)
        {
            return;
        }

        inputBox.Dispatcher.BeginInvoke(
            DispatcherPriority.Input,
            new Action(() =>
            {
                if (inputBox.IsEnabled && inputBox.IsVisible)
                {
                    inputBox.Focus();
                }
            }));
    }

    /// Envía al ViewModel de Output la línea
    /// sobre la que se hizo doble clic.
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

        output.TryActivateLine(e.LineNumber);
    }
}
