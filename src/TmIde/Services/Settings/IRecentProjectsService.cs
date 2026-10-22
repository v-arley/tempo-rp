using System.Collections.Generic;

namespace TmIde.Services.Settings;

/// <summary>Historial de proyectos abiertos que muestra la ventana de inicio.</summary>

public interface IRecentProjectsService
{
    /// <summary>Proyectos recientes, el último abierto primero.</summary>
    IReadOnlyList<RecentProjectEntry> GetAll();

    /// <summary>Registra que se abrió el proyecto de <paramref name="projectDirectory"/>.</summary>
    void Add(string projectDirectory);
}
