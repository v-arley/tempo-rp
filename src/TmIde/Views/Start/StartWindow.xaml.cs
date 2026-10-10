using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using TmIde.ViewModels.Start;

namespace TmIde.Views.Start;

/// <summary>
/// Ventana modal de inicio con chrome propio (WindowStyle=None). Se cierra
/// cuando el ViewModel lo pide (<see cref="StartViewModel.RequestClose"/>) y se
/// arrastra desde la barra de título.
/// </summary>

public partial class StartWindow : Window
{
    public StartWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        NewProjectNameBox.IsVisibleChanged += OnNewProjectNameBoxIsVisibleChanged;
    }

    // Al mostrarse el formulario de nuevo proyecto, el foco va al nombre. Se
    // difiere porque el cambio de visibilidad llega antes de que sea enfocable.
    private void OnNewProjectNameBoxIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () => NewProjectNameBox.Focus());
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is StartViewModel oldViewModel)
        {
            oldViewModel.RequestClose -= OnRequestClose;
            oldViewModel.RequestHelp -= OnRequestHelp;
        }

        if (e.NewValue is StartViewModel newViewModel)
        {
            newViewModel.RequestClose += OnRequestClose;
            newViewModel.RequestHelp += OnRequestHelp;
        }
    }

    private void OnRequestHelp(object? sender, EventArgs e) =>
        new StartHelpWindow { Owner = this }.ShowDialog();

    private void OnRequestClose(object? sender, EventArgs e) => Close();

    private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }
}
