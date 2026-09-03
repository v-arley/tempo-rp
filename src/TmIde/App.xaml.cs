using System.Windows;

using TmIde.Services;
using TmIde.ViewModels;
using TmIde.Views;

namespace TmIde;

/// <summary>
/// Punto de entrada de la aplicación. Composición manual del árbol de objetos
/// en <see cref="OnStartup"/> (sin contenedor de inyección de dependencias):
/// se construye el servicio de tema, el ViewModel raíz y la ventana principal.
/// </summary>
public partial class App : Application
{
    private ThemeService? _themeService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _themeService = new ThemeService();
        _themeService.Apply(AppTheme.Light);

        var mainViewModel = new MainWindowViewModel(_themeService);

        var window = new MainWindow
        {
            DataContext = mainViewModel,
        };

        MainWindow = window;
        window.Show();
    }
}
