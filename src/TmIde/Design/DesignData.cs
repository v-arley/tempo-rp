using System.Collections.Generic;

using CommunityToolkit.Mvvm.Messaging;

using Microsoft.Extensions.Logging.Abstractions;

using TmIde.Services;
using TmIde.Services.Analysis;
using TmIde.Services.Execution;
using TmIde.Services.Navigation;
using TmIde.Services.Settings;
using TmIde.ViewModels;
using TmIde.ViewModels.Editor;
using TmIde.ViewModels.Explorer;
using TmIde.ViewModels.Panel;
using TmIde.ViewModels.Particles;
using TmIde.ViewModels.Shell;

namespace TmIde.Design;

/// <summary>
/// Instancias de solo diseño para <c>d:DataContext="{x:Static design:DesignData.*}"</c>.
/// Dan IntelliSense de bindings y vista previa en el diseñador sin tocar recursos
/// de aplicación reales.
/// </summary>

public static class DesignData
{
    public static MainWindowViewModel MainWindow { get; } = CreateMainWindow();

    public static FileExplorerViewModel Explorer => MainWindow.Explorer;

    public static EditorHostViewModel Editor => MainWindow.EditorHost;

    public static BottomPanelViewModel BottomPanel => MainWindow.BottomPanel;

    public static StatusBarViewModel StatusBar => MainWindow.StatusBar;

    public static ParticlePanelViewModel ParticlePanel => MainWindow.ParticlePanel;

    // El mismo grafo que arma el contenedor en App, con servicios vacíos y sin
    // análisis real.
    private static MainWindowViewModel CreateMainWindow()
    {
        var messenger = new WeakReferenceMessenger();
        var bottomPanel = new BottomPanelViewModel();
        var editorHost = new EditorHostViewModel(new NoopCodeAnalysisService(), messenger);
        var explorer = new FileExplorerViewModel(messenger, new ProjectFileService(), new NoopNamePromptService(), new OutputLog(bottomPanel));
        var runtimePulse = new IdeRuntimePulse();
        var workspace = new ProjectWorkspaceViewModel(
            new NoopStartDialogService(),
            new NoopConfirmDialogService(),
            editorHost,
            explorer,
            bottomPanel,
            new NoopRecentProjectsService(),
            runtimePulse,
            messenger,
            NullLogger<ProjectWorkspaceViewModel>.Instance);

        return new MainWindowViewModel(
            workspace,
            new MenuBarViewModel(new NoopThemeService(), new NoopFlowService(), workspace.Commands, new OutputLog(bottomPanel), explorer),
            explorer,
            editorHost,
            bottomPanel,
            new StatusBarViewModel(workspace.Commands, messenger),
            new ParticlePanelViewModel(runtimePulse, messenger),
            messenger);
    }

    private sealed class NoopRecentProjectsService : IRecentProjectsService
    {
        public IReadOnlyList<RecentProjectEntry> GetAll() => [];

        public void Add(string projectDirectory)
        {
        }
    }

    private sealed class NoopCodeAnalysisService : ICodeAnalysisService
    {
        public DocumentAnalysis Analyze(string filePath, IReadOnlyDictionary<string, string> openDocuments) => DocumentAnalysis.Empty;
    }

    private sealed class NoopThemeService : IThemeService
    {
        public AppTheme Current { get; private set; } = AppTheme.Dark;

        public void ApplySaved()
        {
        }

        public void Apply(AppTheme theme) => Current = theme;
    }

    private sealed class NoopFlowService : IFlowService
    {
        public void ShowMain<TViewModel>(TViewModel viewModel)
            where TViewModel : class
        {
        }

        public void ShowDialog<TViewModel>(TViewModel viewModel)
            where TViewModel : class
        {
        }

        public void Shutdown()
        {
        }
    }

    private sealed class NoopStartDialogService : IStartDialogService
    {
        public string? Show() => null;
    }

    private sealed class NoopConfirmDialogService : IConfirmDialogService
    {
        public bool Confirm(string message) => true;
    }

    private sealed class NoopNamePromptService : INamePromptService
    {
        public string? RequestName(
            string title,
            string message,
            string initialName = "")
        {
            return null;
        }
    }
}
