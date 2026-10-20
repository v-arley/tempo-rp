using System.Windows;

using TmIde.Services;
using TmIde.ViewModels;
using TmIde.ViewModels.Panel;
using TmIde.Views;

namespace TmIde;

/// <summary>
/// Punto de entrada de la aplicación:
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

        var bottomPanel = new BottomPanelViewModel();
        var startDialog = new StartDialogService(bottomPanel.Log);
        var mainViewModel = new MainWindowViewModel(_themeService, startDialog, bottomPanel);

        var window = new MainWindow
        {
            DataContext = mainViewModel,
        };

        MainWindow = window;
        window.Show();
    }
}
