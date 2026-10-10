using CommunityToolkit.Mvvm.ComponentModel;

using TmIde.ViewModels;

namespace TmIde.ViewModels.Start;

/// <summary>Entrada del historial de proyectos abiertos recientemente.</summary>

public sealed partial class RecentProjectViewModel : ViewModelBase
{
    /// <summary>Entrada seleccionada en la lista (su detalle está a la vista).</summary>
    [ObservableProperty]
    private bool _isSelected;

    public RecentProjectViewModel(string name, string path, string lastOpened)
    {
        Name = name;
        Path = path;
        LastOpened = lastOpened;
    }

    public string Name { get; }

    public string Path { get; }

    /// <summary>Inicial del nombre, para el monograma del detalle.</summary>
    public string Initial => Name.Length > 0 ? char.ToUpperInvariant(Name[0]).ToString() : "?";

    /// <summary>Carpetas de la ruta, para mostrarla como migas.</summary>
    public string[] PathSegments => Path.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);

    /// <summary>Texto ya formateado de la última apertura (p. ej. "hace 2 días").</summary>
    public string LastOpened { get; }
}
