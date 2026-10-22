using CommunityToolkit.Mvvm.Input;

namespace TmIde.ViewModels;

/// <summary>
/// Comandos del proyecto abierto. Los crea <see cref="ProjectWorkspaceViewModel"/>
/// y los comparten el menú y la barra de estado, que solo los muestran.
/// </summary>

public sealed record ProjectCommands(
    IRelayCommand ShowStart,
    IRelayCommand Save,
    IRelayCommand Compile,
    IRelayCommand Run,
    IRelayCommand Stop);
