
namespace TmIde.ViewModels.Panel;

/// <summary>
/// Representa la ubicación de un diagnóstico
/// dentro de un archivo del proyecto Tempo.
/// </summary>
public sealed record DiagnosticNavigationTarget(
    string FilePath,
    int Line,
    int? Column);
