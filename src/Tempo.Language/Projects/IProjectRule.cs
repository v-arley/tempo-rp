using Tempo.Language.Diagnostics;

namespace Tempo.Language.Projects;

/// <summary>
/// Una regla estructural del proyecto (§6.6). Cada regla comprueba una sola
/// condición y reporta sus propios diagnósticos; <see cref="ProjectRules.All"/>
/// las enumera todas.
/// </summary>

public interface IProjectRule
{
    void Check(TempoProject project, ProjectDiagnostics diagnostics);
}
