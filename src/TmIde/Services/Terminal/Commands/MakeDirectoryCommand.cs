using System.IO;

namespace TmIde.Services.Terminal.Commands;

/// <summary>
/// <c>mkdir &lt;nombre&gt; [nombre...]</c> — crea uno o más directorios. Los
/// directorios intermedios se crean automáticamente.
/// </summary>

public sealed class MakeDirectoryCommand : ITerminalCommand
{
    public string Name => "mkdir";

    public string Summary => "Crea uno o más directorios.";

    public void Execute(TerminalCommandContext context)
    {
        var created = false;

        foreach (var argument in context.Args)
        {
            if (argument.StartsWith('-'))
            {
                continue;
            }

            var path = context.ResolvePath(argument);
            if (Directory.Exists(path) || File.Exists(path))
            {
                context.WriteError($"mkdir: '{argument}' ya existe");
                continue;
            }

            Directory.CreateDirectory(path);
            created = true;
        }

        if (!created && context.Args.Count == 0)
        {
            context.WriteError("mkdir: falta el nombre del directorio");
        }
    }
}
