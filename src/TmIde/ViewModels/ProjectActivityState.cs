
namespace TmIde.ViewModels;

public enum ProjectActivityState
{
    NoProject,
    Ready,
    Compiling,
    Compiled,
    WithErrors,
    Running
}

/// Notifica a la interfaz cuando cambia el estado del proyecto abierto.
public sealed record ProjectActivityChangedMessage(
    ProjectActivityState State);
