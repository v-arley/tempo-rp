using System.IO;

namespace TmIde.Services.Terminal.Commands;

/// <summary>
/// <c>cat &lt;archivo&gt; [archivo...]</c> (alias <c>type</c>) — vuelca el
/// contenido de uno o más archivos de texto en la terminal.
/// </summary>

public sealed class PrintFileCommand : ITerminalCommand
{
    public string Name => "cat";

    public string Summary => "Muestra el contenido de un archivo de texto.";

    public void Execute(TerminalCommandContext context)
    {
        if (context.Args.Count == 0)
        {
            context.WriteError("cat: falta el nombre del archivo");
            return;
        }

        foreach (var argument in context.Args)
        {
            var path = context.ResolvePath(argument);
            if (!File.Exists(path))
            {
                context.WriteError($"cat: '{argument}' no existe");
                continue;
            }

            foreach (var line in File.ReadLines(path))
            {
                context.WriteLine(line);
            }
        }
    }
}
