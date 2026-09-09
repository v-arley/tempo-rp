namespace TmIde.Services.Terminal;

/// <summary>
/// Un comando de la terminal (p. ej. <c>cd</c>, <c>ls</c>, <c>mkdir</c>). Cada
/// comando es una clase propia; el <see cref="TerminalShell"/> los registra en un
/// diccionario y despacha por <see cref="Name"/>.
/// </summary>

public interface ITerminalCommand
{
    /// <summary>Nombre canónico con el que se invoca el comando.</summary>
    string Name { get; }

    /// <summary>Descripción de una línea que muestra <c>help</c>.</summary>
    string Summary { get; }

    /// <summary>Ejecuta el comando con el contexto ya resuelto (argumentos, shell, salida).</summary>
    void Execute(TerminalCommandContext context);
}
