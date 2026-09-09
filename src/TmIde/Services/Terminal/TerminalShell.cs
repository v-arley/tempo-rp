using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using TmIde.Services.Terminal.Commands;

namespace TmIde.Services.Terminal;

/// <summary>
/// Shell mínimo de la terminal del IDE. Mantiene el directorio de trabajo y el
/// historial, registra los comandos disponibles y despacha una línea de entrada
/// al comando correspondiente. Opera sobre el sistema de archivos real
/// (<see cref="System.IO"/>) con guardas para no borrar rutas sensibles.
/// </summary>

public sealed class TerminalShell
{
    private readonly Dictionary<string, ITerminalCommand> _byName = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ITerminalCommand> _registered = new();
    private readonly List<string> _history = new();

    public TerminalShell(string? initialDirectory = null)
    {
        WorkingDirectory = NormalizeStartDirectory(initialDirectory);
        PreviousDirectory = WorkingDirectory;

        Register(new ChangeDirectoryCommand());
        Register(new ListDirectoryCommand(), "dir");
        Register(new PrintWorkingDirectoryCommand());
        Register(new MakeDirectoryCommand());
        Register(new RemoveCommand(), "del");
        Register(new MoveCommand());
        Register(new CopyCommand(), "copy");
        Register(new PrintFileCommand(), "type");
        Register(new EchoCommand());
        Register(new ClearCommand(), "cls");
        Register(new HelpCommand());
    }

    /// <summary>Directorio de trabajo actual (ruta absoluta y existente).</summary>
    public string WorkingDirectory { get; private set; }

    /// <summary>Directorio de trabajo anterior, para <c>cd -</c>.</summary>
    public string PreviousDirectory { get; private set; }

    /// <summary>Líneas ejecutadas, en orden. No incluye entradas vacías.</summary>
    public IReadOnlyList<string> History => _history;

    /// <summary>Comandos registrados (uno por instancia; sin contar los alias).</summary>
    public IReadOnlyList<ITerminalCommand> Commands => _registered;

    /// <summary>
    /// Ejecuta una línea de entrada. Nunca lanza: los fallos de E/S se envían a
    /// <paramref name="writeError"/> con el prefijo del comando.
    /// </summary>
    public void Execute(string commandLine, Action<string> writeLine, Action<string> writeError, Action clear)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return;
        }

        _history.Add(commandLine);

        var tokens = CommandLineParser.Parse(commandLine);
        if (tokens.Count == 0)
        {
            return;
        }

        var name = tokens[0];
        if (!_byName.TryGetValue(name, out var command))
        {
            writeError($"'{name}' no se reconoce como un comando.");
            return;
        }

        var args = tokens.Skip(1).ToArray();
        var context = new TerminalCommandContext(this, args, writeLine, writeError, clear);

        try
        {
            command.Execute(context);
        }
        catch (Exception ex) when (
            ex is IOException
                or UnauthorizedAccessException
                or ArgumentException
                or NotSupportedException
                or System.Security.SecurityException)
        {
            writeError($"{command.Name}: {ex.Message}");
        }
    }

    /// <summary>Convierte <paramref name="value"/> en ruta absoluta relativa al directorio de trabajo.</summary>
    public string ResolvePath(string value)
    {
        var expanded = ExpandHome(value);
        var combined = Path.IsPathRooted(expanded)
            ? expanded
            : Path.Combine(WorkingDirectory, expanded);

        return Path.GetFullPath(combined);
    }

    /// <summary>
    /// <c>true</c> si <paramref name="path"/> es la raíz de una unidad, el
    /// directorio de trabajo actual o un ancestro suyo: rutas que la terminal se
    /// niega a eliminar.
    /// </summary>
    public bool IsProtectedPath(string path)
    {
        var target = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        var root = Path.TrimEndingDirectorySeparator(Path.GetPathRoot(target) ?? string.Empty);

        if (target.Length == 0 || string.Equals(target, root, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var current = Path.TrimEndingDirectorySeparator(WorkingDirectory);
        if (string.Equals(target, current, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return current.StartsWith(target + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Fija el directorio de trabajo. La ruta debe ser absoluta y existir.</summary>
    internal void SetWorkingDirectory(string fullPath)
    {
        PreviousDirectory = WorkingDirectory;
        WorkingDirectory = Path.TrimEndingDirectorySeparator(fullPath);
    }

    private void Register(ITerminalCommand command, params string[] aliases)
    {
        _registered.Add(command);
        _byName[command.Name] = command;

        foreach (var alias in aliases)
        {
            _byName[alias] = command;
        }
    }

    private static string ExpandHome(string value)
    {
        if (value == "~")
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        if (value.StartsWith("~/", StringComparison.Ordinal) || value.StartsWith(@"~\", StringComparison.Ordinal))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, value[2..]);
        }

        return value;
    }

    private static string NormalizeStartDirectory(string? initialDirectory)
    {
        var candidate = initialDirectory ?? Environment.CurrentDirectory;

        try
        {
            if (Directory.Exists(candidate))
            {
                return Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidate));
            }
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or NotSupportedException)
        {
            // Cae al directorio base de la aplicación.
        }

        return Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
    }
}
