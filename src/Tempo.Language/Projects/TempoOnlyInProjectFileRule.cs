using Tempo.Language.Diagnostics;
using Tempo.Language.Syntax;

namespace Tempo.Language.Projects;

/// <summary>
/// La declaración <c>tempo</c> solo existe en el archivo <c>.tmpr</c> y como
/// máximo una vez: E041 por cada una en un módulo <c>.tm</c> y por
/// cada declaración adicional en el <c>.tmpr</c>.
/// </summary>

public sealed class TempoOnlyInProjectFileRule : IProjectRule
{
    public void Check(TempoProject project, ProjectDiagnostics diagnostics)
    {
        foreach (SyntaxTree file in project.Files.Where(file => file.IsParsed))
        {
            if (file.IsModule)
            {
                foreach (TempoDeclarationNode tempo in file.Root.AllTempos)
                {
                    diagnostics.In(file.FileName).ReportError(
                        DiagnosticCodes.TempoNotAllowed,
                        tempo.Line,
                        $"El módulo '{file.FileName}' declara 'tempo'.",
                        "Mueva la declaración 'tempo' al archivo .tmpr del proyecto.");
                }

                continue;
            }

            foreach (TempoDeclarationNode duplicate in file.Root.AdditionalTempos)
            {
                diagnostics.In(file.FileName).ReportError(
                    DiagnosticCodes.TempoNotAllowed,
                    duplicate.Line,
                    "El archivo de proyecto declara 'tempo' más de una vez.",
                    "Conserve una sola declaración 'tempo' en el archivo .tmpr.");
            }
        }
    }
}
