using System;
using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;

using TmIde.Services.Settings;
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

    /// <summary>Entrada del historial guardado, con la fecha relativa a <paramref name="now"/>.</summary>
    public static RecentProjectViewModel From(RecentProjectEntry entry, DateTimeOffset now) =>
        new(System.IO.Path.GetFileName(entry.Path), entry.Path, FormatLastOpened(entry.LastOpened, now));

    /// <summary>"hoy", "ayer", "hace N días", "hace N semanas" o la fecha (dd/MM/yyyy).</summary>
    public static string FormatLastOpened(DateTimeOffset lastOpened, DateTimeOffset now)
    {
        var days = (now.Date - lastOpened.ToOffset(now.Offset).Date).Days;

        return days switch
        {
            <= 0 => "hoy",
            1 => "ayer",
            < 7 => $"hace {days} días",
            < 14 => "hace 1 semana",
            < 31 => $"hace {days / 7} semanas",
            _ => lastOpened.ToOffset(now.Offset).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
        };
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
