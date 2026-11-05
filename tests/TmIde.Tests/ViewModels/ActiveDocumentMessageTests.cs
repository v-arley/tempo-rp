using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using NSubstitute;

using TmIde.Services;
using TmIde.Services.Analysis;
using TmIde.Services.Execution;
using TmIde.Services.Navigation;
using TmIde.ViewModels;
using TmIde.ViewModels.Editor;
using TmIde.ViewModels.Explorer;
using TmIde.ViewModels.Panel;
using TmIde.ViewModels.Particles;
using TmIde.ViewModels.Shell;

using Xunit;

namespace TmIde.Tests.ViewModels;

/// <summary>
/// <see cref="ActiveDocumentChangedMessage"/>: el editor avisa el cambio de
/// pestaña y la barra de estado y el título lo reflejan sin conocer al editor.
/// </summary>

public sealed class ActiveDocumentMessageTests
{
    private readonly IMessenger _messenger = new WeakReferenceMessenger();

    [Fact]
    public void StatusBar_NoDocument_ShowsNoPosition()
    {
        var statusBar = new StatusBarViewModel(Commands(), _messenger);

        Assert.Equal("LN -, COL -", statusBar.CaretPosition);
    }

    [Fact]
    public void StatusBar_ActiveDocument_FollowsItsCaret()
    {
        var statusBar = new StatusBarViewModel(Commands(), _messenger);
        var document = new EditorDocumentViewModel(@"C:\demo\principal.tmpr", "demo/principal.tmpr", "session > <\n");

        _messenger.Send(new ActiveDocumentChangedMessage(document));
        document.CaretLine = 3;
        document.CaretColumn = 7;

        Assert.Equal("LN 3, COL 7", statusBar.CaretPosition);
    }

    [Fact]
    public void StatusBar_DocumentReplaced_StopsFollowingThePreviousOne()
    {
        var statusBar = new StatusBarViewModel(Commands(), _messenger);
        var first = new EditorDocumentViewModel(@"C:\demo\a.tm", "demo/a.tm", string.Empty);
        var second = new EditorDocumentViewModel(@"C:\demo\b.tm", "demo/b.tm", string.Empty);

        _messenger.Send(new ActiveDocumentChangedMessage(first));
        _messenger.Send(new ActiveDocumentChangedMessage(second));
        first.CaretLine = 9;

        Assert.Equal("LN 1, COL 1", statusBar.CaretPosition);
    }

    [Fact]
    public void MainWindow_TitleIsTheActiveFileName()
    {
        var editorHost = new EditorHostViewModel(Substitute.For<ICodeAnalysisService>(), _messenger);
        var mainWindow = CreateMainWindow(editorHost);
        Assert.Equal("TMIDE", mainWindow.Title);

        using var project = TempDirectory.Create();
        editorHost.OpenDocument(project.WriteFile("principal.tmpr", "session > <\n"));

        Assert.Equal("PRINCIPAL.TMPR", mainWindow.Title);

        editorHost.SelectedDocument!.Close();

        Assert.Equal("TMIDE", mainWindow.Title);
    }

    private MainWindowViewModel CreateMainWindow(EditorHostViewModel editorHost)
    {
        var bottomPanel = new BottomPanelViewModel();
        var commands = Commands();

        var output = Substitute.For<IOutputLog>();

        var explorer = new FileExplorerViewModel(
            _messenger,
            new ProjectFileService(),
            Substitute.For<INamePromptService>(),
            output);

        var menuBar = new MenuBarViewModel(
            Substitute.For<IThemeService>(),
            Substitute.For<IFlowService>(),
            commands,
            output,
            explorer);

        return new MainWindowViewModel(
            workspace: null!,
            menuBar,
            explorer,
            editorHost,
            bottomPanel,
            new StatusBarViewModel(commands, _messenger),
            new ParticlePanelViewModel(new IdeRuntimePulse(), _messenger),
            _messenger);
    }

    private static ProjectCommands Commands()
    {
        var command = new RelayCommand(() => { });
        return new ProjectCommands(command, command, command, command, command);
    }
}
