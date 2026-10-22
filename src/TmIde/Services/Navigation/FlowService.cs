using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

using Microsoft.Extensions.DependencyInjection;

namespace TmIde.Services.Navigation;

/// <summary>
/// Implementación de <see cref="IFlowService"/> sobre WPF. Cada tipo de
/// ViewModel se asocia a un tipo de ventana con <see cref="Register{TViewModel, TWindow}"/>;
/// la ventana se resuelve del contenedor en cada apertura (sin caché), así
/// cada diálogo nace limpio.
/// </summary>

public sealed class FlowService : IFlowService
{
    private readonly IServiceProvider _services;
    private readonly Dictionary<Type, Type> _windowTypes = [];

    public FlowService(IServiceProvider services)
    {
        _services = services;
    }

    /// <summary>
    /// Asocia <typeparamref name="TViewModel"/> a la ventana que lo muestra.
    /// La ventana debe estar registrada en el contenedor.
    /// </summary>
    public FlowService Register<TViewModel, TWindow>()
        where TViewModel : class
        where TWindow : Window
    {
        _windowTypes[typeof(TViewModel)] = typeof(TWindow);
        return this;
    }

    public void ShowMain<TViewModel>(TViewModel viewModel)
        where TViewModel : class
    {
        var window = CreateWindow(viewModel);
        var application = Application.Current;

        application.MainWindow = window;
        application.ShutdownMode = ShutdownMode.OnMainWindowClose;
        window.Show();
    }

    public void ShowDialog<TViewModel>(TViewModel viewModel)
        where TViewModel : class
    {
        var window = CreateWindow(viewModel);

        // Sin dueño (el diálogo de inicio al arrancar) no hay a quién centrarse
        // ni ventana en la barra de tareas que lo represente.
        if (ActiveWindow(window) is { } owner)
        {
            window.Owner = owner;
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.ShowInTaskbar = true;
        }

        window.ShowDialog();
    }

    public void Shutdown() => Application.Current.Shutdown();

    private Window CreateWindow<TViewModel>(TViewModel viewModel)
        where TViewModel : class
    {
        if (!_windowTypes.TryGetValue(typeof(TViewModel), out var windowType))
        {
            throw new InvalidOperationException($"No hay ventana registrada para {typeof(TViewModel).Name}.");
        }

        var window = (Window)_services.GetRequiredService(windowType);
        window.DataContext = viewModel;

        if (viewModel is ICloseable closeable)
        {
            void OnRequestClose(object? sender, EventArgs e) => window.Close();

            closeable.RequestClose += OnRequestClose;
            window.Closed += (_, _) => closeable.RequestClose -= OnRequestClose;
        }

        return window;
    }

    private static Window? ActiveWindow(Window except) =>
        Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(window => window != except && window.IsActive)
        ?? (Application.Current.MainWindow is { IsVisible: true } main && main != except ? main : null);
}
