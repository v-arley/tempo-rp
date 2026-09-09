namespace TmIde.Services.Terminal.Commands;

/// <summary><c>clear</c> (alias <c>cls</c>) — vacía la salida visible de la terminal.</summary>

public sealed class ClearCommand : ITerminalCommand
{
    public string Name => "clear";

    public string Summary => "Limpia la terminal.";

    public void Execute(TerminalCommandContext context) => context.Clear();
}
