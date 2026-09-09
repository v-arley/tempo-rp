using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TmIde.Services.Terminal.Commands;

/// <summary>
/// <c>ls [ruta]</c> (alias <c>dir</c>) — lista el contenido de un directorio:
/// primero las carpetas (con <c>/</c> final) y luego los archivos, en orden
/// alfabético.
/// </summary>

public sealed class ListDirectoryCommand : ITerminalCommand
{
    public string Name => "ls";

    public string Summary => "Lista archivos y carpetas del directorio indicado o el actual.";

    public void Execute(TerminalCommandContext context)
    {
        var operand = context.Args.FirstOrDefault(argument => !argument.StartsWith('-'));
        var path = operand is null ? context.Shell.WorkingDirectory : context.ResolvePath(operand);

        if (File.Exists(path))
        {
            context.WriteLine(Path.GetFileName(path)!);
            return;
        }

        if (!Directory.Exists(path))
        {
            context.WriteError($"ls: no existe la ruta '{operand ?? path}'");
            return;
        }

        var entries = new List<string>();

        foreach (var directory in Directory.GetDirectories(path).OrderBy(entry => Path.GetFileName(entry)!, StringComparer.OrdinalIgnoreCase))
        {
            entries.Add(Path.GetFileName(directory) + "/");
        }

        foreach (var file in Directory.GetFiles(path).OrderBy(entry => Path.GetFileName(entry)!, StringComparer.OrdinalIgnoreCase))
        {
            entries.Add(Path.GetFileName(file)!);
        }

        if (entries.Count == 0)
        {
            context.WriteLine("(directorio vacío)");
            return;
        }

        foreach (var entry in entries)
        {
            context.WriteLine(entry);
        }
    }
}
