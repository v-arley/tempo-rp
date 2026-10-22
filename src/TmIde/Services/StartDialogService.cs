using System;
using System.IO;
using System.Linq;

using Microsoft.Win32;

using TmIde.Services.Navigation;
using TmIde.Services.Settings;
using TmIde.ViewModels.Start;

namespace TmIde.Services;

/// <summary>
/// Abre la ventana de inicio como diálogo modal (vía <see cref="IFlowService"/>)
/// y devuelve el proyecto elegido. Los avisos del diálogo van al panel Output.
/// </summary>

public sealed class StartDialogService : IStartDialogService
{
    private readonly IFlowService _flow;
    private readonly IProjectScaffolder _scaffolder;
    private readonly IOutputLog _output;
    private readonly IRecentProjectsService _recentProjects;
    private readonly TimeProvider _time;

    public StartDialogService(
        IFlowService flow,
        IProjectScaffolder scaffolder,
        IOutputLog output,
        IRecentProjectsService recentProjects,
        TimeProvider time)
    {
        _flow = flow;
        _scaffolder = scaffolder;
        _output = output;
        _recentProjects = recentProjects;
        _time = time;
    }

    public string? Show()
    {
        var now = _time.GetLocalNow();
        var recents = _recentProjects.GetAll().Select(entry => RecentProjectViewModel.From(entry, now));
        var viewModel = new StartViewModel(_output, _scaffolder, PickFolder, recents);
        _flow.ShowDialog(viewModel);

        return viewModel.ProjectToOpen;
    }

    // Sin dueño explícito el diálogo nativo toma la ventana activa: la de inicio.
    private static string? PickFolder(string initialDirectory)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Seleccionar carpeta",
            InitialDirectory = Directory.Exists(initialDirectory) ? initialDirectory : string.Empty,
        };

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}
