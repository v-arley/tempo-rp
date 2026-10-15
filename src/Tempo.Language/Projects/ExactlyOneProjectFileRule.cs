using Tempo.Language.Diagnostics;
using Tempo.Language.Syntax;

namespace Tempo.Language.Projects;

/// <summary>
/// Un proyecto abierto desde un directorio tiene exactamente un archivo
/// <c>.tmpr</c>: E016 si no hay ninguno, E017 por cada archivo adicional.
/// </summary>

public sealed class ExactlyOneProjectFileRule : IProjectRule
{
    public void Check(TempoProject project, ProjectDiagnostics diagnostics)
    {
        if (!project.IsWholeDirectory)
        {
            return;
        }

        List<SyntaxTree> projectFiles = project.ProjectFiles.ToList();

        if (projectFiles.Count == 0)
        {
            diagnostics.In(Path.GetFileName(project.RootDirectory)).ReportError(
                DiagnosticCodes.MissingProjectFile,
                1,
                "El directorio del proyecto no contiene ningún archivo .tmpr.",
                "Agregue un archivo .tmpr con la sentencia 'session' como punto de entrada.");

            return;
        }

        foreach (SyntaxTree extra in projectFiles.Skip(1))
        {
            diagnostics.In(extra.FileName).ReportError(
                DiagnosticCodes.MultipleProjectFiles,
                1,
                $"El proyecto ya tiene el archivo principal '{projectFiles[0].FileName}'.",
                "Deje un solo archivo .tmpr y convierta los demás en módulos .tm.");
        }
    }
}
