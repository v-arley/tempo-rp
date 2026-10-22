using System;
using System.IO;

namespace TmIde.Tests;

/// <summary>Carpeta temporal propia de una prueba; se borra al liberarla.</summary>

public sealed class TempDirectory : IDisposable
{
    private TempDirectory(string path)
    {
        Path = path;
    }

    public string Path { get; }

    public static TempDirectory Create()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TmIde.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return new TempDirectory(path);
    }

    /// <summary>Escribe un archivo (creando sus carpetas) y devuelve su ruta completa.</summary>
    public string WriteFile(string relativePath, string content)
    {
        var fullPath = System.IO.Path.Combine(Path, relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
        return fullPath;
    }

    public string CreateDirectory(string relativePath) =>
        Directory.CreateDirectory(System.IO.Path.Combine(Path, relativePath)).FullName;

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // Un archivo aún abierto no debe hacer fallar la prueba.
        }
    }
}
