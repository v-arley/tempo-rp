namespace TmIde.Services.Terminal.Commands;

/// <summary><c>pwd</c> — imprime la ruta absoluta del directorio de trabajo.</summary>

public sealed class PrintWorkingDirectoryCommand : ITerminalCommand
{
    public string Name => "pwd";

    public string Summary => "Imprime la ruta del directorio de trabajo actual.";

    public void Execute(TerminalCommandContext context) => context.WriteLine(context.Shell.WorkingDirectory);
}
