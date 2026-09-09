using System.IO;

namespace TmIde.Services.Terminal.Commands;

/// <summary>
/// <c>cd [ruta]</c> — cambia el directorio de trabajo. Sin argumentos imprime el
/// actual; <c>cd -</c> vuelve al anterior; admite <c>~</c>, rutas relativas y
/// absolutas.
/// </summary>

public sealed class ChangeDirectoryCommand : ITerminalCommand
{
    public string Name => "cd";

    public string Summary => "Cambia el directorio de trabajo (cd .., cd ~, cd -).";

    public void Execute(TerminalCommandContext context)
    {
        if (context.Args.Count == 0)
        {
            context.WriteLine(context.Shell.WorkingDirectory);
            return;
        }

        var raw = context.Args[0];
        var target = raw == "-"
            ? context.Shell.PreviousDirectory
            : context.ResolvePath(raw);

        if (!Directory.Exists(target))
        {
            context.WriteError($"cd: no existe el directorio '{raw}'");
            return;
        }

        context.Shell.SetWorkingDirectory(target);
    }
}
