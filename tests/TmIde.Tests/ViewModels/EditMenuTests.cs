using System.Linq;

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using NSubstitute;

using TmIde.Services;
using TmIde.Services.Analysis;
using TmIde.Services.Navigation;
using TmIde.ViewModels;
using TmIde.ViewModels.Editor;
using TmIde.ViewModels.Explorer;
using TmIde.ViewModels.Shell;

using Xunit;

namespace TmIde.Tests.ViewModels;

/// <summary>
/// Menú EDITAR: cada entrada pide su acción al editor visible por
/// <see cref="EditorHostViewModel.ActionRequest"/>; sin archivo abierto lo avisa.
/// </summary>

public sealed class EditMenuTests
{
    private readonly WeakReferenceMessenger _messenger = new();
    private readonly IOutputLog _output = Substitute.For<IOutputLog>();
    private readonly EditorHostViewModel _editorHost;
    private readonly MenuBarViewModel _menuBar;

    public EditMenuTests()
    {
        var analysis = Substitute.For<ICodeAnalysisService>();
        analysis.Analyze(default!, default!).ReturnsForAnyArgs(DocumentAnalysis.Empty);
        _editorHost = new EditorHostViewModel(analysis, _messenger);

        var command = new RelayCommand(() => { });
        var explorer = new FileExplorerViewModel(_messenger, new ProjectFileService(), Substitute.For<INamePromptService>(), _output);

        _menuBar = new MenuBarViewModel(
            Substitute.For<IThemeService>(),
            Substitute.For<IFlowService>(),
            new ProjectCommands(command, command, command, command, command),
            _output,
            explorer,
            editorHost: _editorHost);
    }

    private void Click(string header) =>
        _menuBar.Roots.Single(root => root.Header == "EDITAR").Items.Single(item => item.Header.StartsWith(header, System.StringComparison.Ordinal)).Command!.Execute(null);

    [Fact]
    public void EditMenu_ComesRightAfterTheMainMenu()
    {
        Assert.Equal(["MENU", "EDITAR"], _menuBar.Roots.Take(2).Select(root => root.Header));
    }

    [Theory]
    [InlineData("Buscar", EditorAction.Find)]
    [InlineData("Reemplazar", EditorAction.Replace)]
    [InlineData("Autocompletar", EditorAction.Complete)]
    public void Entry_WithAnOpenFile_RequestsItsAction(string header, EditorAction expected)
    {
        using var project = TempDirectory.Create();
        _editorHost.OpenDocument(project.WriteFile("principal.tmpr", "session > <\n"));

        Click(header);

        Assert.Equal(expected, _editorHost.ActionRequest?.Action);
    }

    [Fact]
    public void Entry_RepeatedTwice_ProducesANewRequestEachTime()
    {
        using var project = TempDirectory.Create();
        _editorHost.OpenDocument(project.WriteFile("principal.tmpr", "session > <\n"));

        Click("Buscar");
        var first = _editorHost.ActionRequest;
        Click("Buscar");

        Assert.NotSame(first, _editorHost.ActionRequest);
    }

    [Fact]
    public void Entry_WithoutAnOpenFile_WarnsInsteadOfRequesting()
    {
        Click("Reemplazar");

        Assert.Null(_editorHost.ActionRequest);
        _output.Received(1).WriteLine("EDITAR → Abre un archivo primero.");
    }
}
