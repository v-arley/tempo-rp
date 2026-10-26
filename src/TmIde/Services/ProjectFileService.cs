using System;
using System.IO;
using System.Linq;

using Tempo.Language.Diagnostics;

namespace TmIde.Services;

/// <summary>
/// Gestiona los archivos y carpetas de un proyecto Tempo.
/// </summary>
public sealed class ProjectFileService : IProjectFileService
{
    private const string MainExtension = ".tmpr";
    private const string ModuleExtension = ".tm";

    private static readonly char[] ForbiddenCharacters =
        "<>:\"/\\|?*".ToCharArray();

    public string CreateFile(
        string projectDirectory,
        string targetDirectory,
        string fileName)
    {
        var root = GetProjectRoot(projectDirectory);
        var directory = RequireDirectory(root, targetDirectory);

        ValidateName(fileName);
        ValidateExtension(fileName);
        ValidateMainFile(root, directory, fileName, null);
        EnsureAvailable(directory, fileName);

        var path = Path.Combine(directory, fileName);

        using var stream = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);

        using var writer = new StreamWriter(stream);

        if (IsMainFile(fileName))
        {
            writer.WriteLine("session >");
            writer.WriteLine("    output >> \"Hola, Mundo\";");
            writer.WriteLine("<");
        }

        return path;
    }

    public string CreateFolder(
        string projectDirectory,
        string targetDirectory,
        string folderName)
    {
        var root = GetProjectRoot(projectDirectory);
        var directory = RequireDirectory(root, targetDirectory);

        ValidateName(folderName);
        EnsureAvailable(directory, folderName);

        var path = Path.Combine(directory, folderName);

        Directory.CreateDirectory(path);

        return path;
    }

    public string Rename(
        string projectDirectory,
        string originalPath,
        string newName)
    {
        var root = GetProjectRoot(projectDirectory);
        var path = RequireExistingChild(root, originalPath);

        ValidateName(newName);

        var isDirectory = Directory.Exists(path);

        if (!isDirectory)
        {
            ValidateExtension(newName);
        }

        var parent = Path.GetDirectoryName(path)!;
        var newPath = Path.Combine(parent, newName);

        if (string.Equals(path, newPath, StringComparison.Ordinal))
        {
            return path;
        }

        if (!isDirectory)
        {
            ValidateMainFile(root, parent, newName, path);
        }

        EnsureAvailable(parent, newName, path);

        if (isDirectory)
        {
            Directory.Move(path, newPath);
        }
        else
        {
            File.Move(path, newPath);
        }

        return newPath;
    }

    public void Delete(string projectDirectory, string path)
    {
        var root = GetProjectRoot(projectDirectory);
        var child = RequireExistingChild(root, path);

        if (File.Exists(child))
        {
            File.Delete(child);
        }
        else
        {
            Directory.Delete(child, recursive: true);
        }
    }

    private static string GetProjectRoot(string projectDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);

        var root = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(projectDirectory));

        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException(
                $"No existe la carpeta del proyecto: {root}");
        }

        return root;
    }

    private static string RequireDirectory(
        string root,
        string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        var fullPath = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(directory));

        EnsureInsideProject(root, fullPath, true);

        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException(
                $"No existe la carpeta: {fullPath}");
        }

        return fullPath;
    }

    private static string RequireExistingChild(
        string root,
        string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(path));

        EnsureInsideProject(root, fullPath, false);

        if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"No existe el archivo o carpeta: {fullPath}",
                fullPath);
        }

        return fullPath;
    }

    private static void EnsureInsideProject(
        string root,
        string path,
        bool includeRoot)
    {
        var relative = Path.GetRelativePath(root, path);

        var isRoot = relative == ".";

        var isOutside =
            Path.IsPathRooted(relative) ||
            relative == ".." ||
            relative.StartsWith(
                $"..{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal);

        if (isOutside || (isRoot && !includeRoot))
        {
            throw new InvalidOperationException(
                "La operación debe realizarse dentro del proyecto.");
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) ||
            name != name.Trim() ||
            name is "." or ".." ||
            name.EndsWith('.') ||
            name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            name.IndexOfAny(ForbiddenCharacters) >= 0 ||
            name.Any(char.IsControl))
        {
            throw new ArgumentException(
                "El nombre está vacío o contiene caracteres inválidos.",
                nameof(name));
        }

        var stem = name.Split('.')[0];

        if (stem.Equals("CON", StringComparison.OrdinalIgnoreCase) ||
            stem.Equals("PRN", StringComparison.OrdinalIgnoreCase) ||
            stem.Equals("AUX", StringComparison.OrdinalIgnoreCase) ||
            stem.Equals("NUL", StringComparison.OrdinalIgnoreCase) ||
            (stem.Length == 4 &&
             (stem.StartsWith("COM", StringComparison.OrdinalIgnoreCase) ||
              stem.StartsWith("LPT", StringComparison.OrdinalIgnoreCase)) &&
             stem[3] is >= '1' and <= '9'))
        {
            throw new ArgumentException(
                "El nombre está reservado por Windows.",
                nameof(name));
        }
    }

    private static void ValidateExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);

        if (!extension.Equals(
                ModuleExtension,
                StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(
                MainExtension,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Los archivos deben tener extensión .tm o .tmpr.",
                nameof(fileName));
        }
    }

    private static void EnsureAvailable(
        string directory,
        string name,
        string? originalPath = null)
    {
        var conflict = Directory.EnumerateFileSystemEntries(directory)
            .FirstOrDefault(path =>
                string.Equals(
                    Path.GetFileName(path),
                    name,
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    path,
                    originalPath,
                    StringComparison.OrdinalIgnoreCase));

        if (conflict is not null)
        {
            throw new IOException(
                $"Ya existe un archivo o carpeta llamado '{name}'.");
        }
    }

    private static void ValidateMainFile(
        string root,
        string directory,
        string fileName,
        string? originalPath)
    {
        if (!IsMainFile(fileName))
        {
            return;
        }

        if (!string.Equals(
                root,
                directory,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "El archivo .tmpr debe estar en la raíz del proyecto.");
        }

        var alreadyExists = Directory.EnumerateFiles(root)
            .Any(path =>
                IsMainFile(path) &&
                !string.Equals(
                    path,
                    originalPath,
                    StringComparison.OrdinalIgnoreCase));

        if (alreadyExists)
        {
            throw new InvalidOperationException(
                $"{DiagnosticCodes.MultipleProjectFiles}: " +
                "El proyecto ya tiene un archivo principal .tmpr.");
        }
    }

    private static bool IsMainFile(string path)
    {
        return Path.GetExtension(path).Equals(
            MainExtension,
            StringComparison.OrdinalIgnoreCase);
    }
}