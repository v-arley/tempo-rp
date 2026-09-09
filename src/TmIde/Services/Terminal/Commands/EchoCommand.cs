using System;

namespace TmIde.Services.Terminal.Commands;

/// <summary><c>echo &lt;texto&gt;</c> — repite en la terminal el texto recibido.</summary>

public sealed class EchoCommand : ITerminalCommand
{
    public string Name => "echo";

    public string Summary => "Escribe en la terminal el texto indicado.";

    public void Execute(TerminalCommandContext context) => context.WriteLine(string.Join(' ', context.Args));
}
