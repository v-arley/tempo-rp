using System;

namespace TmIde.Services.Navigation;

/// <summary>
/// ViewModel que puede pedir que se cierre su ventana sin conocerla.
/// <see cref="FlowService"/> escucha <see cref="RequestClose"/> y cierra la
/// ventana que abrió para él.
/// </summary>

public interface ICloseable
{
    event EventHandler? RequestClose;
}
