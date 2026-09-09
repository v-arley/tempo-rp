using System.IO;

namespace TmIde.Services.Terminal.Commands;

/// <summary>
/// <c>cp [-r] &lt;origen&gt; &lt;destino&gt;</c> (alias <c>copy</c>) — copia un
/// archivo o, con <c>-r</c>, un directorio completo. Si el destino es un
/// directorio existente, el origen se copia dentro de él.
/// </summary>

public sealed class CopyCommand : ITerminalCommand
{
    public string Name => "cp";

    public string Summary => "Copia un archivo; con -r, un directorio completo.";

    public void Execute(TerminalCommandContext context)
    {
        var recursive = false;
        string? sourceArg = null;
        string? destinationArg = null;

        foreach (var argument in context.Args)
        {
            if (argument is "-r" or "-R")
            {
                recursive = true;
            }
            else if (sourceArg is null)
            {
                sourceArg = argument;
            }
            else
            {
                destinationArg ??= argument;
            }
        }

        if (sourceArg is null || destinationArg is null)
        {
            context.WriteError("cp: se requieren origen y destino");
            return;
        }

        var source = context.ResolvePath(sourceArg);
        var destination = context.ResolvePath(destinationArg);

        if (File.Exists(source))
        {
            if (Directory.Exists(destination))
            {
                destination = Path.Combine(destination, Path.GetFileName(source));
            }

            File.Copy(source, destination, overwrite: true);
            return;
        }

        if (Directory.Exists(source))
        {
            if (!recursive)
            {
                context.WriteError($"cp: '{sourceArg}' es un directorio (usa -r)");
                return;
            }

            if (Directory.Exists(destination))
            {
                destination = Path.Combine(destination, Path.GetFileName(Path.TrimEndingDirectorySeparator(source)));
            }

            CopyDirectory(source, destination);
            return;
        }

        context.WriteError($"cp: '{sourceArg}' no existe");
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
        }

        foreach (var directory in Directory.GetDirectories(source))
        {
            CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(Path.TrimEndingDirectorySeparator(directory))));
        }
    }
}
