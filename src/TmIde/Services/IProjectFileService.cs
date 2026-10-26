namespace TmIde.Services;

/// <summary>
/// Define las operaciones sobre archivos y carpetas
/// de un proyecto Tempo.
/// </summary>
public interface IProjectFileService
{
    
    string CreateFile(
        string projectDirectory,
        string targetDirectory,
        string fileName);

    string CreateFolder(
        string projectDirectory,
        string targetDirectory,
        string folderName);

    string Rename(
        string projectDirectory,
        string originalPath,
        string newName);

    void Delete(
        string projectDirectory,
        string path);
}