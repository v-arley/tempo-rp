using System.Collections.ObjectModel;

using TmIde.Services;
using TmIde.Services.Navigation;
using TmIde.ViewModels;
using TmIde.ViewModels.Explorer;
using CommunityToolkit.Mvvm.Input;

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
    private readonly FileExplorerViewModel _explorer;
    private readonly ILanguageReferenceService? _languageReference;

    public MenuBarViewModel(IThemeService themeService, IFlowService flow, ProjectCommands commands, IOutputLog output, FileExplorerViewModel explorer, 
        ILanguageReferenceService? languageReference = null)
    {
        _themeService = themeService;
        _flow = flow;
        _commands = commands;
        _output = output;
        _explorer = explorer;
        _languageReference = languageReference;

        ShowLanguageReferenceCommand = new RelayCommand(() => _languageReference?.Show());

        Roots = new ObservableCollection<MenuEntryViewModel>
        {
            BuildFileMenu(),
            BuildToolsMenu(),
            BuildViewMenu(),
            BuildOtherMenu(),
        };
    }

    public ObservableCollection<MenuEntryViewModel> Roots { get; }
    public IRelayCommand ShowLanguageReferenceCommand { get; }

    private MenuEntryViewModel BuildFileMenu()
    {
        var menu = new MenuEntryViewModel("MENU");
        menu.Add(new MenuEntryViewModel("Inicio", _commands.ShowStart));
        menu.Add(new MenuEntryViewModel("Nuevo archivo", () => _explorer.CreateFileCommand.Execute(null)));
        menu.Add(new MenuEntryViewModel("Guardar (Ctrl+S)", _commands.Save));
        menu.Add(new MenuEntryViewModel("Salir", _flow.Shutdown));
        return menu;
    }

    private MenuEntryViewModel BuildToolsMenu()
    {
        var menu = new MenuEntryViewModel("EJECUCIÓN");
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
        var menu = new MenuEntryViewModel("AYUDA");

        menu.Add(new MenuEntryViewModel("Referencia del lenguaje (F1)", ShowLanguageReferenceCommand));

        menu.Add(new MenuEntryViewModel("Acerca de TmIde", () => _output.WriteLine("TmIde — Entorno de desarrollo del lenguaje Tempo")));

        return menu;
    }
}
