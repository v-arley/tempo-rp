using System.IO;

namespace TmIde.Services.Terminal.Commands;

/// <summary>
/// <c>mv &lt;origen&gt; &lt;destino&gt;</c> — mueve o renombra un archivo o
/// directorio. Si el destino es un directorio existente, el origen se mueve
/// dentro de él conservando su nombre.
/// </summary>

public sealed class MoveCommand : ITerminalCommand
{
    public string Name => "mv";

    public string Summary => "Mueve o renombra un archivo o directorio.";

    public void Execute(TerminalCommandContext context)
    {
        if (context.Args.Count < 2)
        {
            context.WriteError("mv: se requieren origen y destino");
            return;
        }

        var source = context.ResolvePath(context.Args[0]);
        var destination = context.ResolvePath(context.Args[1]);

        if (Directory.Exists(destination))
        {
            var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(source));
            destination = Path.Combine(destination, name);
        }

        if (File.Exists(source))
        {
            File.Move(source, destination, overwrite: true);
        }
        else if (Directory.Exists(source))
        {
            Directory.Move(source, destination);
        }
        else
        {
            context.WriteError($"mv: '{context.Args[0]}' no existe");
        }
    }
}
