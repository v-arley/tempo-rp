using System.Collections.ObjectModel;

using TmIde.Services;
using TmIde.Services.Navigation;
using TmIde.ViewModels;

namespace TmIde.ViewModels.Shell;

/// <summary>
/// Barra de menú del título. Las entradas del proyecto (Inicio, Guardar,
/// Compilar, Ejecutar, Detener) usan <see cref="ProjectCommands"/>; Salir y el
/// cambio de tema (menú VER) también funcionan; el resto son stubs que escriben
/// una línea en el panel inferior.
/// </summary>

public sealed partial class MenuBarViewModel : ViewModelBase
{
    private readonly IThemeService _themeService;
    private readonly IFlowService _flow;
    private readonly ProjectCommands _commands;
    private readonly IOutputLog _output;

    public MenuBarViewModel(IThemeService themeService, IFlowService flow, ProjectCommands commands, IOutputLog output)
    {
        _themeService = themeService;
        _flow = flow;
        _commands = commands;
        _output = output;

        Roots = new ObservableCollection<MenuEntryViewModel>
        {
            BuildFileMenu(),
            BuildToolsMenu(),
            BuildViewMenu(),
            BuildOtherMenu(),
        };
    }

    public ObservableCollection<MenuEntryViewModel> Roots { get; }

    private MenuEntryViewModel BuildFileMenu()
    {
        var menu = new MenuEntryViewModel("MENU");
        menu.Add(new MenuEntryViewModel("Inicio", _commands.ShowStart));
        menu.Add(new MenuEntryViewModel("Nuevo archivo", () => _output.WriteLine("MENU → Nuevo archivo (pendiente)")));
        menu.Add(new MenuEntryViewModel("Guardar (Ctrl+S)", _commands.Save));
        menu.Add(new MenuEntryViewModel("Salir", _flow.Shutdown));
        return menu;
    }

    private MenuEntryViewModel BuildToolsMenu()
    {
        var menu = new MenuEntryViewModel("Ejecucion");
        menu.Add(new MenuEntryViewModel("Compilar", _commands.Compile));
        menu.Add(new MenuEntryViewModel("Ejecutar", _commands.Run));
        menu.Add(new MenuEntryViewModel("Detener", _commands.Stop));
        return menu;
    }

    private MenuEntryViewModel BuildViewMenu()
    {
        var menu = new MenuEntryViewModel("VER");
        menu.Add(new MenuEntryViewModel("Alternar tema claro / oscuro", () =>
        {
            _themeService.Toggle();
            _output.WriteLine($"VER → Tema activo: {_themeService.Current}");
        }));
        return menu;
    }

    private MenuEntryViewModel BuildOtherMenu()
    {
        var menu = new MenuEntryViewModel("OTROS");
        menu.Add(new MenuEntryViewModel("Acerca de TmIde", () => _output.WriteLine("TmIde — esqueleto de UI para el lenguaje TM (v0)")));
        return menu;
    }
}
