namespace TmIde.Services;

/// <summary>
/// Crea la estructura en disco de un proyecto TM nuevo: una carpeta con el
/// archivo principal <c>.tmpr</c> (el único que puede contener <c>session</c>).
/// </summary>

public interface IProjectScaffolder
{
    /// <summary>Nombre del archivo principal que se genera en la carpeta del proyecto.</summary>
    string MainFileName { get; }

    /// <summary>
    /// Comprueba si se puede crear el proyecto <paramref name="name"/> dentro de
    /// <paramref name="location"/>. Devuelve el motivo si no es posible, o
    /// <c>null</c> si lo es.
    /// </summary>
    string? Validate(string location, string name);

    /// <summary>Ruta de la carpeta que tendría el proyecto.</summary>
    string GetProjectPath(string location, string name);

    /// <summary>Crea la carpeta y el archivo principal; devuelve la ruta del proyecto.</summary>
    string Create(string location, string name);
}
