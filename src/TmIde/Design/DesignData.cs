using TmIde.Services;
using TmIde.ViewModels;
using TmIde.ViewModels.Editor;
using TmIde.ViewModels.Explorer;
using TmIde.ViewModels.Panel;

namespace TmIde.Design;

/// <summary>
/// Instancias de solo diseño para <c>d:DataContext="{x:Static design:DesignData.*}"</c>.
/// Dan IntelliSense de bindings y vista previa en el diseñador sin tocar recursos
/// de aplicación reales.
/// </summary>

public static class DesignData
{
    public static MainWindowViewModel MainWindow { get; } = new(
        new NoopThemeService(),
        new NoopStartDialogService(),
        new BottomPanelViewModel(),
        _ => true);

    public static FileExplorerViewModel Explorer => MainWindow.Explorer;

    public static EditorHostViewModel Editor => MainWindow.EditorHost;

    public static BottomPanelViewModel BottomPanel => MainWindow.BottomPanel;

    public static StatusBarViewModel StatusBar => MainWindow.StatusBar;

    private sealed class NoopThemeService : IThemeService
    {
        public AppTheme Current { get; private set; } = AppTheme.Light;

        public void Apply(AppTheme theme) => Current = theme;

        public void Toggle() => Current = Current == AppTheme.Light ? AppTheme.Dark : AppTheme.Light;
    }

    private sealed class NoopStartDialogService : IStartDialogService
    {
        public string? Show() => null;
    }
}
