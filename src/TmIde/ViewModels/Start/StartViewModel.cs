using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using TmIde.Services;
using TmIde.Services.Navigation;
using TmIde.ViewModels;

namespace TmIde.ViewModels.Start;

/// <summary>
/// Ventana modal de inicio: arriba las acciones (nuevo proyecto | abrir
/// proyecto), en el centro el historial de proyectos recientes y abajo el
/// detalle del proyecto seleccionado. Un primer clic en un reciente lo
/// selecciona y muestra su detalle; un segundo clic sobre el mismo lo abre.
/// "Nuevo proyecto" cambia la ventana al formulario <see cref="NewProjectForm"/>,
/// que crea el proyecto en disco. Abrir, crear o activar un reciente deja la
/// carpeta en <see cref="ProjectToOpen"/> y cierra el diálogo.
/// </summary>

public sealed partial class StartViewModel : ViewModelBase, ICloseable
{
    private readonly IOutputLog _output;
    private readonly IProjectScaffolder _scaffolder;
    private readonly Func<string, string?> _pickFolder;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedProject))]
    private RecentProjectViewModel? _selectedProject;

    /// <summary>Formulario de nuevo proyecto; <c>null</c> en la vista de inicio.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNewProjectMode))]
    [NotifyPropertyChangedFor(nameof(HeaderTitle))]
    private NewProjectViewModel? _newProjectForm;

    /// <param name="pickFolder">Selector de carpetas para la ubicación del proyecto nuevo.</param>
    /// <param name="recentProjects">Historial guardado, el más reciente primero.</param>
    public StartViewModel(
        IOutputLog output,
        IProjectScaffolder scaffolder,
        Func<string, string?> pickFolder,
        IEnumerable<RecentProjectViewModel> recentProjects)
    {
        _output = output;
        _scaffolder = scaffolder;
        _pickFolder = pickFolder;

        RecentProjects = new ObservableCollection<RecentProjectViewModel>(recentProjects);
    }

    /// <summary>Se dispara cuando el diálogo debe cerrarse; el servicio de flujo lo escucha.</summary>
    public event EventHandler? RequestClose;

    /// <summary>Se dispara al pedir la guía de inicio; la vista abre su ventana.</summary>
    public event EventHandler? RequestHelp;

    public ObservableCollection<RecentProjectViewModel> RecentProjects { get; }

    /// <summary>Muestra el mensaje de historial vacío.</summary>
    public bool IsRecentEmpty => RecentProjects.Count == 0;

    /// <summary>Muestra el detalle del proyecto seleccionado.</summary>
    public bool HasSelectedProject => SelectedProject is not null;

    /// <summary>La ventana muestra el formulario de nuevo proyecto.</summary>
    public bool IsNewProjectMode => NewProjectForm is not null;

    /// <summary>Texto de la barra superior según la vista activa.</summary>
    public string HeaderTitle => IsNewProjectMode ? "INICIO / NUEVO PROYECTO" : "INICIO";

    public string AppVersion => "v0.1 · desarrollo";

    public string RuntimeVersion => $".NET {Environment.Version}";

    public string CompilerStatus => "Léxico · Sintaxis · Semántica · Ejecución";

    /// <summary>Carpeta del proyecto elegido al cerrar el diálogo; <c>null</c> si no se eligió ninguno.</summary>
    public string? ProjectToOpen { get; private set; }

    [RelayCommand]
    private void OpenProject()
    {
        if (_pickFolder(string.Empty) is { } folder)
        {
            OpenAndClose(folder);
        }
    }

    [RelayCommand]
    private void NewProject() =>
        NewProjectForm = new NewProjectViewModel(_scaffolder, _pickFolder, OnProjectCreated, () => NewProjectForm = null);

    private void OnProjectCreated(string projectPath)
    {
        _output.WriteLine($"INICIO → Proyecto creado: {projectPath}");
        OpenAndClose(projectPath);
    }

    private void OpenAndClose(string projectPath)
    {
        ProjectToOpen = projectPath;
        Close();
    }

    /// <summary>Esc: desde el formulario vuelve al inicio; desde el inicio cierra.</summary>
    [RelayCommand]
    private void Back()
    {
        if (IsNewProjectMode)
        {
            NewProjectForm = null;
            return;
        }

        Close();
    }

    /// <summary>
    /// Clic sobre un reciente: si no es el seleccionado lo selecciona (muestra
    /// su detalle); si ya lo era, lo abre.
    /// </summary>
    [RelayCommand]
    private void ActivateRecent(RecentProjectViewModel? project)
    {
        if (project is null)
        {
            return;
        }

        if (!ReferenceEquals(project, SelectedProject))
        {
            SelectedProject = project;
            return;
        }

        if (!Directory.Exists(project.Path))
        {
            _output.WriteLine($"INICIO → El proyecto ya no existe: {project.Path}");
            return;
        }

        OpenAndClose(project.Path);
    }

    partial void OnSelectedProjectChanged(RecentProjectViewModel? oldValue, RecentProjectViewModel? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.IsSelected = false;
        }

        if (newValue is not null)
        {
            newValue.IsSelected = true;
        }
    }

    [RelayCommand]
    private void ShowHelp() => RequestHelp?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void Close() => RequestClose?.Invoke(this, EventArgs.Empty);
}
