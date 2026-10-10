using System;
using System.IO;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using TmIde.Services;
using TmIde.ViewModels;

namespace TmIde.ViewModels.Start;

/// <summary>
/// Formulario "Nuevo proyecto" de la ventana de inicio: nombre + ubicación,
/// vista previa de lo que se va a crear y validación en vivo. Al crear delega
/// en <see cref="IProjectScaffolder"/> y avisa a la ventana de inicio con la
/// ruta del proyecto; si falla muestra el error sin cerrar el formulario.
/// </summary>

public sealed partial class NewProjectViewModel : ViewModelBase
{
    private readonly IProjectScaffolder _scaffolder;
    private readonly Func<string, string?> _pickFolder;
    private readonly Action<string> _onCreated;
    private readonly Action _onCancelled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProjectPath))]
    [NotifyPropertyChangedFor(nameof(MainFilePath))]
    [NotifyPropertyChangedFor(nameof(ValidationMessage))]
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    [NotifyCanExecuteChangedFor(nameof(CreateCommand))]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProjectPath))]
    [NotifyPropertyChangedFor(nameof(MainFilePath))]
    [NotifyPropertyChangedFor(nameof(ValidationMessage))]
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    [NotifyCanExecuteChangedFor(nameof(CreateCommand))]
    private string _location;

    /// <summary>Error de la última creación (E/S, permisos); se limpia al editar.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ValidationMessage))]
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    private string? _creationError;

    /// <param name="pickFolder">Abre el selector de carpetas partiendo de la ruta dada; <c>null</c> si se cancela.</param>
    public NewProjectViewModel(
        IProjectScaffolder scaffolder,
        Func<string, string?> pickFolder,
        Action<string> onCreated,
        Action onCancelled)
    {
        _scaffolder = scaffolder;
        _pickFolder = pickFolder;
        _onCreated = onCreated;
        _onCancelled = onCancelled;

        _location = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    /// <summary>Carpeta que se va a crear (vista previa).</summary>
    public string ProjectPath => string.IsNullOrWhiteSpace(Name)
        ? string.Empty
        : _scaffolder.GetProjectPath(Location, Name);

    /// <summary>Archivo principal que se va a crear (vista previa).</summary>
    public string MainFilePath => string.IsNullOrWhiteSpace(Name)
        ? _scaffolder.MainFileName
        : Path.Combine(ProjectPath, _scaffolder.MainFileName);

    /// <summary>
    /// Motivo por el que no se puede crear. Con el nombre aún vacío no se
    /// muestra nada (el botón de crear ya está deshabilitado).
    /// </summary>
    public string? ValidationMessage => CreationError
        ?? (string.IsNullOrWhiteSpace(Name) ? null : _scaffolder.Validate(Location, Name));

    public bool HasValidationMessage => ValidationMessage is not null;

    partial void OnNameChanged(string value) => CreationError = null;

    partial void OnLocationChanged(string value) => CreationError = null;

    [RelayCommand]
    private void Browse()
    {
        if (_pickFolder(Location) is { } folder)
        {
            Location = folder;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCreate))]
    private void Create()
    {
        try
        {
            _onCreated(_scaffolder.Create(Location, Name));
        }
        catch (IOException ex)
        {
            CreationError = $"No se pudo crear el proyecto: {ex.Message}";
        }
        catch (UnauthorizedAccessException ex)
        {
            CreationError = $"Sin permisos para crear el proyecto: {ex.Message}";
        }
    }

    private bool CanCreate() => _scaffolder.Validate(Location, Name) is null;

    [RelayCommand]
    private void Cancel() => _onCancelled();
}
