using System;
using System.IO;
using System.Linq;

using NSubstitute;

using TmIde.Services;
using TmIde.ViewModels.Start;

using Xunit;

namespace TmIde.Tests.ViewModels;

/// <summary>
/// Diálogo de inicio sin ventana: el selector de carpetas es una función de
/// prueba y el cierre se observa por <see cref="StartViewModel.RequestClose"/>.
/// </summary>

public sealed class StartViewModelTests : IDisposable
{
    private readonly TempDirectory _directory = TempDirectory.Create();
    private readonly IOutputLog _output = Substitute.For<IOutputLog>();
    private string? _pickedFolder;
    private int _closeRequests;

    public void Dispose() => _directory.Dispose();

    [Fact]
    public void OpenProject_FolderChosen_ReturnsItAndCloses()
    {
        _pickedFolder = _directory.Path;
        var viewModel = CreateViewModel();

        viewModel.OpenProjectCommand.Execute(null);

        Assert.Equal(_directory.Path, viewModel.ProjectToOpen);
        Assert.Equal(1, _closeRequests);
    }

    [Fact]
    public void OpenProject_PickerCancelled_StaysOpen()
    {
        var viewModel = CreateViewModel();

        viewModel.OpenProjectCommand.Execute(null);

        Assert.Null(viewModel.ProjectToOpen);
        Assert.Equal(0, _closeRequests);
    }

    [Fact]
    public void ActivateRecent_FirstClickSelects_SecondClickOpens()
    {
        var viewModel = CreateViewModel();
        var recent = new RecentProjectViewModel("demo", _directory.Path, "hoy");
        viewModel.RecentProjects.Add(recent);

        viewModel.ActivateRecentCommand.Execute(recent);

        Assert.Same(recent, viewModel.SelectedProject);
        Assert.True(recent.IsSelected);
        Assert.Null(viewModel.ProjectToOpen);

        viewModel.ActivateRecentCommand.Execute(recent);

        Assert.Equal(_directory.Path, viewModel.ProjectToOpen);
        Assert.Equal(1, _closeRequests);
    }

    [Fact]
    public void ActivateRecent_FolderNoLongerExists_ReportsItAndStaysOpen()
    {
        var missing = Path.Combine(_directory.Path, "borrado");
        var viewModel = CreateViewModel();
        var recent = new RecentProjectViewModel("borrado", missing, "ayer");

        viewModel.ActivateRecentCommand.Execute(recent);
        viewModel.ActivateRecentCommand.Execute(recent);

        Assert.Null(viewModel.ProjectToOpen);
        Assert.Equal(0, _closeRequests);
        _output.Received(1).WriteLine($"INICIO → El proyecto ya no existe: {missing}");
    }

    [Fact]
    public void NewProject_Created_OpensItAndCloses()
    {
        var viewModel = CreateViewModel();
        viewModel.NewProjectCommand.Execute(null);
        var form = viewModel.NewProjectForm!;
        form.Location = _directory.Path;
        form.Name = "demo";

        form.CreateCommand.Execute(null);

        Assert.NotNull(viewModel.ProjectToOpen);
        Assert.True(Directory.Exists(viewModel.ProjectToOpen));
        Assert.Equal(1, _closeRequests);
        _output.Received(1).WriteLine($"INICIO → Proyecto creado: {viewModel.ProjectToOpen}");
    }

    [Fact]
    public void Back_FromNewProjectForm_ReturnsToStart()
    {
        var viewModel = CreateViewModel();
        viewModel.NewProjectCommand.Execute(null);

        viewModel.BackCommand.Execute(null);

        Assert.False(viewModel.IsNewProjectMode);
        Assert.Equal(0, _closeRequests);
    }

    [Fact]
    public void Back_FromStart_Closes()
    {
        var viewModel = CreateViewModel();

        viewModel.BackCommand.Execute(null);

        Assert.Equal(1, _closeRequests);
    }

    [Fact]
    public void RecentProjects_ShowTheSavedHistoryInOrder()
    {
        var viewModel = CreateViewModel(
            new RecentProjectViewModel("b", @"C:\b", "hoy"),
            new RecentProjectViewModel("a", @"C:\a", "ayer"));

        Assert.Equal(["b", "a"], viewModel.RecentProjects.Select(project => project.Name));
        Assert.False(viewModel.IsRecentEmpty);
    }

    [Fact]
    public void RecentProjects_NoHistory_IsEmpty()
    {
        Assert.True(CreateViewModel().IsRecentEmpty);
    }

    private StartViewModel CreateViewModel(params RecentProjectViewModel[] recentProjects)
    {
        var viewModel = new StartViewModel(_output, new ProjectScaffolder(), _ => _pickedFolder, recentProjects);
        viewModel.RequestClose += (_, _) => _closeRequests++;
        return viewModel;
    }
}
