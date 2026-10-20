using System;
using System.ComponentModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using TmIde.Services;
using TmIde.ViewModels.Editor;
using TmIde.ViewModels.Explorer;
using TmIde.ViewModels.Panel;
using TmIde.ViewModels.Shell;

namespace TmIde.ViewModels;

/// <summary>
/// ViewModel raíz de la ventana principal. Agrega los ViewModels de cada región
/// del shell y los conecta entre sí (el explorador abre documentos en el editor;
/// menús y barra de estado escriben en el panel inferior).
/// </summary>

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IThemeService _themeService;

    [ObservableProperty]
    private string _title = "TMIDE";

    /// <param name="bottomPanel">
    /// Se recibe ya creado porque otros servicios (el diálogo de inicio) también
    /// escriben en él.
    /// </param>
    public MainWindowViewModel(IThemeService themeService, IStartDialogService startDialog, BottomPanelViewModel bottomPanel)
    {
        _themeService = themeService;

        EditorHost = new EditorHostViewModel();
        BottomPanel = bottomPanel;
        Explorer = new FileExplorerViewModel();
        MenuBar = new MenuBarViewModel(themeService, startDialog, BottomPanel.Log);
        StatusBar = new StatusBarViewModel(EditorHost);

        EditorHost.PropertyChanged += OnEditorHostPropertyChanged;
        UpdateTitle();
    }

    public MenuBarViewModel MenuBar { get; }

    public FileExplorerViewModel Explorer { get; }

    public EditorHostViewModel EditorHost { get; }

    public BottomPanelViewModel BottomPanel { get; }

    public StatusBarViewModel StatusBar { get; }

    [RelayCommand]
    private void ToggleTheme()
    {
        _themeService.Toggle();
        BottomPanel.Log($"Tema activo: {_themeService.Current}");
    }

    private void OnEditorHostPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorHostViewModel.SelectedDocument))
        {
            UpdateTitle();
        }
    }

    private void UpdateTitle()
    {
        Title = EditorHost.SelectedDocument is { } document
            ? document.FileName.ToUpperInvariant()
            : "TMIDE";
    }
}
