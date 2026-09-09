using System;
using System.Linq;

namespace TmIde.Services.Terminal.Commands;

/// <summary><c>help</c> — lista los comandos disponibles con su descripción.</summary>

public sealed class HelpCommand : ITerminalCommand
{
    public string Name => "help";

    public string Summary => "Muestra esta ayuda.";

    public void Execute(TerminalCommandContext context)
    {
        context.WriteLine("Comandos disponibles:");

        foreach (var command in context.Shell.Commands.OrderBy(entry => entry.Name, StringComparer.Ordinal))
        {
            context.WriteLine($"  {command.Name,-7} {command.Summary}");
        }
    }
}
