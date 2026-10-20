using System;
using System.IO;

namespace TmIde.Services;

/// <summary>
/// Genera un proyecto TM mínimo en disco (ver ESPECIFICACION-TM, 6.6):
/// <code>
/// &lt;ubicación&gt;/&lt;nombre&gt;/
///   |-- principal.tmpr   # session con un "Hola, Mundo"
/// </code>
/// </summary>

public sealed class ProjectScaffolder : IProjectScaffolder
{
    private static readonly string MainFileTemplate = string.Join(
        Environment.NewLine,
        "session >",
        "    output >> \"Hola, Mundo\";",
        "<",
        string.Empty);

    public string MainFileName => "principal.tmpr";

    public string? Validate(string location, string name)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(name);

        var trimmedName = name.Trim();
        if (trimmedName.Length == 0)
        {
            return "Escribe un nombre para el proyecto.";
        }

        if (trimmedName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || trimmedName.EndsWith('.'))
        {
            return "El nombre contiene caracteres no válidos para una carpeta.";
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            return "Indica la ubicación del proyecto.";
        }

        if (!Path.IsPathFullyQualified(location) || !Directory.Exists(location))
        {
            return "La ubicación no existe.";
        }

        var projectPath = GetProjectPath(location, name);
        if (Directory.Exists(projectPath) || File.Exists(projectPath))
        {
            return "Ya existe una carpeta con ese nombre en la ubicación.";
        }

        return null;
    }

    public string GetProjectPath(string location, string name)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(name);

        return Path.Combine(location.Trim(), name.Trim());
    }

    public string Create(string location, string name)
    {
        if (Validate(location, name) is { } error)
        {
            throw new InvalidOperationException(error);
        }

        var projectPath = GetProjectPath(location, name);
        Directory.CreateDirectory(projectPath);
        File.WriteAllText(Path.Combine(projectPath, MainFileName), MainFileTemplate);

        return projectPath;
    }
}
