using System;
using System.Windows;

using TmIde.ViewModels.Start;

namespace TmIde.Views.Start;

/// <summary>
/// Ventana modal de inicio con chrome propio (WindowStyle=None). La abre y la
/// cierra <c>FlowService</c>; el arrastre y el foco del formulario los dan
/// Behaviors en el XAML. Aquí solo se abre la guía de ayuda.
/// </summary>

public partial class StartWindow : Window
{
    public StartWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is StartViewModel oldViewModel)
        {
            oldViewModel.RequestHelp -= OnRequestHelp;
        }

        if (e.NewValue is StartViewModel newViewModel)
        {
            newViewModel.RequestHelp += OnRequestHelp;
        }
    }

    private void OnRequestHelp(object? sender, EventArgs e) =>
        new StartHelpWindow { Owner = this }.ShowDialog();
}
