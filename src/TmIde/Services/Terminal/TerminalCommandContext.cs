using System;
using System.Collections.Generic;

namespace TmIde.Services.Terminal;

/// <summary>
/// Todo lo que un <see cref="ITerminalCommand"/> necesita para ejecutarse: los
/// argumentos ya tokenizados, el <see cref="TerminalShell"/> (para el directorio
/// de trabajo y la resolución de rutas) y los tres canales de salida
/// (<see cref="WriteLine"/>, <see cref="WriteError"/>, <see cref="Clear"/>).
/// </summary>

public sealed class TerminalCommandContext
{
    private readonly Action<string> _writeLine;
    private readonly Action<string> _writeError;
    private readonly Action _clear;

    public TerminalCommandContext(
        TerminalShell shell,
        IReadOnlyList<string> args,
        Action<string> writeLine,
        Action<string> writeError,
        Action clear)
    {
        Shell = shell;
        Args = args;
        _writeLine = writeLine;
        _writeError = writeError;
        _clear = clear;
    }

    public TerminalShell Shell { get; }

    /// <summary>Argumentos posicionales y banderas, sin el nombre del comando.</summary>
    public IReadOnlyList<string> Args { get; }

    /// <summary>Emite una línea normal en la terminal.</summary>
    public void WriteLine(string text) => _writeLine(text);

    /// <summary>Emite una línea de error (mismo canal visual, prefijo del comando).</summary>
    public void WriteError(string text) => _writeError(text);

    /// <summary>Vacía el búfer visible de la terminal.</summary>
    public void Clear() => _clear();

    /// <summary>Resuelve <paramref name="value"/> contra el directorio de trabajo actual.</summary>
    public string ResolvePath(string value) => Shell.ResolvePath(value);
}
