using System.Collections.Generic;
using System.IO;

namespace TmIde.Services.Terminal.Commands;

/// <summary>
/// <c>rm [-r] &lt;ruta&gt; [ruta...]</c> (alias <c>del</c>) — elimina archivos y,
/// con <c>-r</c>, directorios completos. Se niega a borrar la raíz de una
/// unidad, el directorio de trabajo o un ancestro suyo.
/// </summary>

public sealed class RemoveCommand : ITerminalCommand
{
    public string Name => "rm";

    public string Summary => "Elimina archivos; con -r, directorios completos.";

    public void Execute(TerminalCommandContext context)
    {
        var recursive = false;
        var targets = new List<string>();

        foreach (var argument in context.Args)
        {
            switch (argument)
            {
                case "-r" or "-R" or "-rf" or "-fr":
                    recursive = true;
                    break;
                case "-f":
                    break;
                default:
                    targets.Add(argument);
                    break;
            }
        }

        if (targets.Count == 0)
        {
            context.WriteError("rm: falta el operando");
            return;
        }

        foreach (var target in targets)
        {
            var path = context.ResolvePath(target);

            if (context.Shell.IsProtectedPath(path))
            {
                context.WriteError($"rm: se niega a eliminar '{target}'");
                continue;
            }

            if (Directory.Exists(path))
            {
                if (!recursive)
                {
                    context.WriteError($"rm: '{target}' es un directorio (usa -r)");
                    continue;
                }

                Directory.Delete(path, recursive: true);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
            else
            {
                context.WriteError($"rm: '{target}' no existe");
            }
        }
    }
}
