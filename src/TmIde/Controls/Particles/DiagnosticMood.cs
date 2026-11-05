using System;
using System.Collections.Generic;
using System.Linq;

using Tempo.Language.Diagnostics;

namespace TmIde.Controls.Particles;

/// <summary>
/// Cómo se ve el proyecto: su <see cref="DiagnosticState"/> y cuánto agita a
/// las partículas (<see cref="Turbulence"/>, de 0 a 1). Se lee como la tabla de
/// la _: <c>DiagnosticMood.From(errores, advertencias)</c>.
/// </summary>

public sealed record DiagnosticMood(DiagnosticState State, double Turbulence)
{
    /// <summary>Con este número de problemas la turbulencia llega a su máximo.</summary>
    public const int ProblemsForFullTurbulence = 10;

    public static DiagnosticMood Correct { get; } = new(DiagnosticState.Correct, 0);

    // Cada estado se agita según sus propios problemas: los errores en Error y
    // las advertencias en Warning.
    public static DiagnosticMood From(int errors, int warnings) => (errors, warnings) switch
    {
        ( > 0, _) => new(DiagnosticState.Error, TurbulenceFor(errors)),
        (_, > 0) => new(DiagnosticState.Warning, TurbulenceFor(warnings)),
        _ => Correct,
    };

    /// <summary>Estado de un proyecto a partir de los diagnósticos de todos sus archivos.</summary>
    public static DiagnosticMood From(IEnumerable<Diagnostic> diagnostics)
    {
        var all = diagnostics.ToList();
        var errors = all.Count(diagnostic => diagnostic.IsError);

        return From(errors, all.Count - errors);
    }

    private static double TurbulenceFor(int problems) =>
        Math.Min(problems, ProblemsForFullTurbulence) / (double)ProblemsForFullTurbulence;
}
