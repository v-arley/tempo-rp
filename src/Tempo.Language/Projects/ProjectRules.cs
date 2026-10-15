namespace Tempo.Language.Projects;

/// <summary>
/// Reglas estructurales que se comprueban sobre todo proyecto, en este orden.
/// Agregar una regla consiste en implementar <see cref="IProjectRule"/> y
/// listarla aquí.
/// </summary>

public static class ProjectRules
{
    public static IReadOnlyList<IProjectRule> All { get; } = new IProjectRule[]
    {
        new ExactlyOneProjectFileRule(),
        new ModulesDeclareNoSessionRule()
    };
}
