using System;
using System.IO;
using System.Linq;

using CommunityToolkit.Mvvm.Messaging;

using NSubstitute;

using TmIde.Services;
using TmIde.Services.Analysis;
using TmIde.ViewModels.Editor;
using TmIde.ViewModels.Explorer;

using Xunit;

namespace TmIde.Tests.ViewModels;

public sealed class FileExplorerViewModelTests : IDisposable
{
    private readonly TempDirectory _project =
        TempDirectory.Create();

    private readonly IMessenger _messenger =
        new WeakReferenceMessenger();

    private readonly INamePromptService _namePrompt =
        Substitute.For<INamePromptService>();

    private readonly IConfirmDialogService _confirmDialog =
        Substitute.For<IConfirmDialogService>();

    private readonly FileExplorerViewModel _explorer;

    private readonly EditorHostViewModel _editorHost;

    public FileExplorerViewModelTests()
    {
        var analysis = Substitute.For<ICodeAnalysisService>();

        analysis
            .Analyze(default!, default!)
            .ReturnsForAnyArgs(DocumentAnalysis.Empty);

        _editorHost = new EditorHostViewModel(
            analysis,
            _messenger);

        _editorHost.SwitchToProject(_project.Path);

        _explorer = new FileExplorerViewModel(
            _messenger,
            new ProjectFileService(),
            _namePrompt,
            Substitute.For<IOutputLog>());

        _explorer.ConfigureFileOperations(
            _editorHost,
            _confirmDialog);

        _explorer.Load(_project.Path);
    }

    public void Dispose()
    {
        _project.Dispose();
    }

    [Fact]
    public void Load_CreatesProjectRoot()
    {
        var root = Assert.Single(_explorer.Roots);

        Assert.Equal(_project.Path, root.FullPath);
        Assert.True(root.IsDirectory);
        Assert.True(root.IsExpanded);
    }

    [Fact]
    public void Load_ShowsProjectFiles()
    {
        _project.WriteFile(
            "principal.tmpr",
            "session > <");

        _project.WriteFile(
            "operaciones.tm",
            "");

        _explorer.Refresh();

        var root = Root();

        Assert.Contains(
            root.Children,
            node => node.Name == "principal.tmpr");

        Assert.Contains(
            root.Children,
            node => node.Name == "operaciones.tm");
    }

    [Fact]
    public void CreateFile_CreatesModuleAndRefreshesTree()
    {
        SetPromptName("nuevo.tm");

        _explorer.CreateFileCommand.Execute(null);

        Assert.True(File.Exists(
            Path.Combine(_project.Path, "nuevo.tm")));

        Assert.Contains(
            Root().Children,
            node => node.Name == "nuevo.tm");
    }

    [Fact]
    public void CreateFolder_CreatesDirectory()
    {
        SetPromptName("modulos");

        _explorer.CreateFolderCommand.Execute(null);

        Assert.True(Directory.Exists(
            Path.Combine(_project.Path, "modulos")));

        Assert.Contains(
            Root().Children,
            node => node.Name == "modulos");
    }

    [Fact]
    public void CreateFile_InsideSelectedFolder_Works()
    {
        _project.CreateDirectory("modulos");

        _explorer.Refresh();

        _explorer.SelectedNode = Root()
            .Children
            .Single(node => node.Name == "modulos");

        SetPromptName("utilidades.tm");

        _explorer.CreateFileCommand.Execute(null);

        Assert.True(File.Exists(
            Path.Combine(
                _project.Path,
                "modulos",
                "utilidades.tm")));
    }

    [Fact]
    public void Refresh_PreservesExpandedFolders()
    {
        _project.CreateDirectory("modulos");

        _explorer.Refresh();

        var folder = Root()
            .Children
            .Single(node => node.Name == "modulos");

        folder.IsExpanded = true;

        _explorer.Refresh();

        var refreshedFolder = Root()
            .Children
            .Single(node => node.Name == "modulos");

        Assert.True(refreshedFolder.IsExpanded);
    }

    [Fact]
    public void RenameSelected_UpdatesFileAndOpenTab()
    {
        var original = _project.WriteFile(
            "operaciones.tm",
            "sample numero = 10;");

        _editorHost.OpenDocument(original);

        _explorer.Refresh();

        _explorer.SelectedNode = Root()
            .Children
            .Single(node => node.Name == "operaciones.tm");

        SetPromptName("utilidades.tm");

        _explorer.RenameSelectedCommand.Execute(null);

        var renamedPath = Path.Combine(
            _project.Path,
            "utilidades.tm");

        Assert.False(File.Exists(original));
        Assert.True(File.Exists(renamedPath));

        var document = Assert.Single(
            _editorHost.Documents);

        Assert.Equal(
            renamedPath,
            document.FilePath);

        Assert.Equal(
            "utilidades.tm",
            document.FileName);
    }

    [Fact]
    public void DeleteSelected_UserCancels_KeepsFile()
    {
        var file = _project.WriteFile(
            "temporal.tm",
            "");

        _explorer.Refresh();

        _explorer.SelectedNode = Root()
            .Children
            .Single(node => node.Name == "temporal.tm");

        _confirmDialog
            .Confirm(Arg.Any<string>())
            .Returns(false);

        _explorer.DeleteSelectedCommand.Execute(null);

        Assert.True(File.Exists(file));
    }

    [Fact]
    public void DeleteSelected_Confirmed_ClosesOpenTab()
    {
        var file = _project.WriteFile(
            "temporal.tm",
            "sample numero = 10;");

        _editorHost.OpenDocument(file);

        _explorer.Refresh();

        _explorer.SelectedNode = Root()
            .Children
            .Single(node => node.Name == "temporal.tm");

        _confirmDialog
            .Confirm(Arg.Any<string>())
            .Returns(true);

        _explorer.DeleteSelectedCommand.Execute(null);

        Assert.False(File.Exists(file));
        Assert.Empty(_editorHost.Documents);
    }

    [Theory]
    [InlineData("principal.tmpr", "IconMainFile")]
    [InlineData("operaciones.tm", "IconFileCode")]
    [InlineData("archivo.js", "IconFile")]
    [InlineData("archivo.ts", "IconFile")]
    [InlineData("archivo.tsx", "IconFile")]
    [InlineData("notas.txt", "IconFile")]
    public void IconKey_UsesCorrectFileIcon(
        string fileName,
        string expectedIcon)
    {
        var node = new FileNodeViewModel(
            Path.Combine(_project.Path, fileName),
            isDirectory: false,
            onOpen: null,
            children: []);

        Assert.Equal(expectedIcon, node.IconKey);
    }

    [Fact]
    public void MainProjectFile_IsIdentified()
    {
        var main = new FileNodeViewModel(
            Path.Combine(_project.Path, "principal.tmpr"),
            isDirectory: false,
            onOpen: null,
            children: []);

        var module = new FileNodeViewModel(
            Path.Combine(_project.Path, "operaciones.tm"),
            isDirectory: false,
            onOpen: null,
            children: []);

        Assert.True(main.IsMainProjectFile);
        Assert.False(module.IsMainProjectFile);
    }

    private FileNodeViewModel Root()
    {
        return Assert.Single(_explorer.Roots);
    }

    private void SetPromptName(string name)
    {
        _namePrompt
            .RequestName(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>())
            .Returns(name);
    }
}