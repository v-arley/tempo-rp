using System;
using System.Collections.ObjectModel;

using TmIde.Services;
using TmIde.ViewModels;

namespace TmIde.ViewModels.Shell;

/// <summary>
/// Barra de menú del título. Funcionan Inicio, Guardar, Salir y el cambio de
/// tema (menú VER); el resto son stubs que escriben una línea en el panel inferior.
/// </summary>

public sealed partial class MenuBarViewModel : ViewModelBase
{
    private readonly IThemeService _themeService;
    private readonly Action _showStart;
    private readonly Action _save;
    private readonly Action<string> _log;

    /// <param name="showStart">Abre la ventana de inicio y carga el proyecto elegido.</param>
    /// <param name="save">Guarda el documento activo.</param>
    public MenuBarViewModel(IThemeService themeService, Action showStart, Action save, Action<string> log)
    {
        _themeService = themeService;
        _showStart = showStart;
        _save = save;
        _log = log;

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
        menu.Add(new MenuEntryViewModel("Inicio", _showStart));
        menu.Add(new MenuEntryViewModel("Nuevo archivo", () => _log("MENU → Nuevo archivo (pendiente)")));
        menu.Add(new MenuEntryViewModel("Guardar (Ctrl+S)", _save));
        menu.Add(new MenuEntryViewModel("Salir", () => System.Windows.Application.Current.Shutdown()));
        return menu;
    }

    private MenuEntryViewModel BuildToolsMenu()
    {
        var menu = new MenuEntryViewModel("Ejecucion");
        menu.Add(new MenuEntryViewModel("Compilar", () => _log("TOOLS → Compilar (pendiente)")));
        return menu;
    }

    private MenuEntryViewModel BuildViewMenu()
    {
        var menu = new MenuEntryViewModel("VER");
        menu.Add(new MenuEntryViewModel("Alternar tema claro / oscuro", () =>
        {
            _themeService.Toggle();
            _log($"VER → Tema activo: {_themeService.Current}");
        }));
        return menu;
    }

    private MenuEntryViewModel BuildOtherMenu()
    {
        var menu = new MenuEntryViewModel("OTROS");
        menu.Add(new MenuEntryViewModel("Acerca de TmIde", () => _log("TmIde — esqueleto de UI para el lenguaje TM (v0)")));
        return menu;
    }
}
