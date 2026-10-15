using Tempo.Language.Diagnostics;
using Tempo.Language.Syntax;

namespace Tempo.Language.Projects;

/// <summary>
/// Un módulo <c>.tm</c> no declara <c>session</c>: el punto de entrada solo
/// existe en el archivo <c>.tmpr</c> (E015).
/// </summary>

public sealed class ModulesDeclareNoSessionRule : IProjectRule
{
    public void Check(TempoProject project, ProjectDiagnostics diagnostics)
    {
        IEnumerable<SyntaxTree> modulesWithSession = project.Modules
            .Where(module => module.IsParsed && module.Root.Session is not null);

        foreach (SyntaxTree module in modulesWithSession)
        {
            diagnostics.In(module.FileName).ReportError(
                DiagnosticCodes.ModuleWithSession,
                1,
                $"El módulo '{module.FileName}' declara una sentencia 'session'.",
                "Mueva el punto de entrada al archivo .tmpr del proyecto.");
        }
    }
}
