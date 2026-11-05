using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Messaging;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using TmIde.Services;
using TmIde.Services.Analysis;
using TmIde.Services.Execution;
using TmIde.Services.Settings;
using TmIde.ViewModels;
using TmIde.ViewModels.Editor;
using TmIde.ViewModels.Explorer;
using TmIde.ViewModels.Panel;

using Xunit;

namespace TmIde.Tests.ViewModels;

/// <summary>
/// Operaciones del proyecto abierto sin ventanas: los diálogos son dobles de
/// prueba y los proyectos, carpetas temporales reales.
/// </summary>

public sealed class ProjectWorkspaceViewModelTests : IDisposable
{
    private const string ValidProgram = "session > output >> 1; <\n";

    private readonly TempDirectory _project = TempDirectory.Create();
    private readonly IStartDialogService _startDialog = Substitute.For<IStartDialogService>();
    private readonly IConfirmDialogService _confirmDialog = Substitute.For<IConfirmDialogService>();
    private readonly IRecentProjectsService _recentProjects = Substitute.For<IRecentProjectsService>();
    private readonly IMessenger _messenger = new WeakReferenceMessenger();
    private readonly EditorHostViewModel _editorHost;
    private readonly FileExplorerViewModel _explorer;
    private readonly BottomPanelViewModel _bottomPanel = new();
    private readonly ProjectWorkspaceViewModel _workspace;

    public ProjectWorkspaceViewModelTests()
    {
        var analysis = Substitute.For<ICodeAnalysisService>();
        analysis.Analyze(default!, default!).ReturnsForAnyArgs([]);
        _editorHost = new EditorHostViewModel(analysis, _messenger);
        _explorer = new FileExplorerViewModel(_messenger, new ProjectFileService(), Substitute.For<INamePromptService>(), Substitute.For<IOutputLog>());

        _workspace = new ProjectWorkspaceViewModel(
            _startDialog,
            _confirmDialog,
            _editorHost,
            _explorer,
            _bottomPanel,
            _recentProjects,
            new IdeRuntimePulse(),
            _messenger,
            NullLogger<ProjectWorkspaceViewModel>.Instance);
    }

    public void Dispose() => _project.Dispose();

    [Fact]
    public void OpenProject_ExistingFolder_LoadsItInTheExplorer()
    {
        _project.WriteFile("principal.tmpr", ValidProgram);

        _workspace.OpenProject(_project.Path);

        Assert.Equal(_project.Path, _workspace.ProjectDirectory);
        var root = Assert.Single(_explorer.Roots);
        Assert.Equal(_project.Path, root.FullPath);
        Assert.Contains($"Proyecto abierto: {_project.Path}", OutputLines());
        _recentProjects.Received(1).Add(_project.Path);
    }

    [Fact]
    public void OpenProject_MissingFolder_ReportsItAndKeepsNoProject()
    {
        var missing = Path.Combine(_project.Path, "no-existe");

        _workspace.OpenProject(missing);

        Assert.Null(_workspace.ProjectDirectory);
        Assert.Contains(OutputLines(), line => line.StartsWith($"No se pudo abrir el proyecto {missing}", StringComparison.Ordinal));
        _recentProjects.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public void OpenProject_UnsavedChangesAndUserDeclines_KeepsTheCurrentProject()
    {
        using var other = TempDirectory.Create();
        OpenProjectWithModifiedDocument();
        _confirmDialog.Confirm(default!).ReturnsForAnyArgs(false);

        _workspace.OpenProject(other.Path);

        Assert.Equal(_project.Path, _workspace.ProjectDirectory);
        Assert.Single(_editorHost.Documents);
        _confirmDialog.ReceivedWithAnyArgs(1).Confirm(default!);
    }

    [Fact]
    public void OpenProject_UnsavedChangesAndUserAccepts_ClosesTheDocuments()
    {
        using var other = TempDirectory.Create();
        OpenProjectWithModifiedDocument();
        _confirmDialog.Confirm(default!).ReturnsForAnyArgs(true);

        _workspace.OpenProject(other.Path);

        Assert.Equal(other.Path, _workspace.ProjectDirectory);
        Assert.Empty(_editorHost.Documents);
    }

    [Fact]
    public void OpenProject_NoUnsavedChanges_DoesNotAskForConfirmation()
    {
        using var other = TempDirectory.Create();
        _workspace.OpenProject(_project.Path);

        _workspace.OpenProject(other.Path);

        _confirmDialog.DidNotReceiveWithAnyArgs().Confirm(default!);
    }

    [Fact]
    public void ExplorerOpenFile_OpensTheDocumentInTheEditor()
    {
        var file = _project.WriteFile("principal.tmpr", ValidProgram);
        _workspace.OpenProject(_project.Path);

        FileNode("principal.tmpr").OpenCommand.Execute(null);

        Assert.Equal(file, _editorHost.SelectedDocument?.FilePath);
    }

    [Fact]
    public void OpenFileRequestedMessage_FromAnySender_OpensTheDocument()
    {
        var file = _project.WriteFile("principal.tmpr", ValidProgram);
        _workspace.OpenProject(_project.Path);

        _messenger.Send(new OpenFileRequestedMessage(file));

        Assert.Equal(file, _editorHost.SelectedDocument?.FilePath);
    }

    [Fact]
    public void ShowStart_ProjectChosen_OpensIt()
    {
        _startDialog.Show().Returns(_project.Path);

        _workspace.Commands.ShowStart.Execute(null);

        Assert.Equal(_project.Path, _workspace.ProjectDirectory);
    }

    [Fact]
    public void ShowStart_DialogCancelled_KeepsNoProject()
    {
        _startDialog.Show().Returns((string?)null);

        _workspace.Commands.ShowStart.Execute(null);

        Assert.Null(_workspace.ProjectDirectory);
    }

    [Fact]
    public async Task Compile_NoProject_AsksToOpenOne()
    {
        await _workspace.CompileCommand.ExecuteAsync(null);

        Assert.Contains("COMPILE → Abre un proyecto antes de compilar.", OutputLines());
    }

    [Fact]
    public async Task Compile_ValidProject_ReportsNoProblems()
    {
        _project.WriteFile("principal.tmpr", ValidProgram);
        _workspace.OpenProject(_project.Path);

        await _workspace.CompileCommand.ExecuteAsync(null);

        Assert.Contains("COMPILE → 0 error(es), 0 advertencia(s).", OutputLines());
    }

    [Fact]
    public async Task Compile_EachTime_StartsFromAnEmptyOutput()
    {
        _project.WriteFile("principal.tmpr", ValidProgram);
        _workspace.OpenProject(_project.Path);

        await _workspace.CompileCommand.ExecuteAsync(null);
        var firstOutput = OutputLines();
        await _workspace.CompileCommand.ExecuteAsync(null);

        Assert.Equal(firstOutput, OutputLines());
    }

    [Fact]
    public async Task Compile_UsesTheOpenDocumentInsteadOfTheDisk()
    {
        // En disco el programa usa una variable sin declarar; en el editor está corregido.
        _project.WriteFile("principal.tmpr", "session > output >> y; <\n");
        _workspace.OpenProject(_project.Path);
        FileNode("principal.tmpr").OpenCommand.Execute(null);
        var document = _editorHost.SelectedDocument!.Document;
        document.Replace(0, document.TextLength, ValidProgram);

        await _workspace.CompileCommand.ExecuteAsync(null);

        Assert.Contains("COMPILE → 0 error(es), 0 advertencia(s).", OutputLines());
    }

    [Fact]
    public async Task Run_ProjectWithErrors_DoesNotExecute()
    {
        _project.WriteFile("principal.tmpr", "session > output >> y; <\n");
        _workspace.OpenProject(_project.Path);

        await _workspace.RunCommand.ExecuteAsync(null);

        Assert.Contains("RUN → Corrige los errores antes de ejecutar.", OutputLines());
        Assert.False(_workspace.IsRunning);
        Assert.Empty(_bottomPanel.Run.Lines);
    }

    [Fact]
    public void Stop_NothingRunning_IsDisabled()
    {
        Assert.False(_workspace.Commands.Stop.CanExecute(null));
    }

    private void OpenProjectWithModifiedDocument()
    {
        _project.WriteFile("principal.tmpr", ValidProgram);
        _workspace.OpenProject(_project.Path);
        FileNode("principal.tmpr").OpenCommand.Execute(null);
        _editorHost.SelectedDocument!.Document.Insert(0, "// cambio\n");

        Assert.True(_editorHost.HasUnsavedChanges);
    }

    private FileNodeViewModel FileNode(string name) =>
        _explorer.Roots.Single().Children.Single(node => node.Name == name);

    private string[] OutputLines() => [.. _bottomPanel.Output.Lines];
}
