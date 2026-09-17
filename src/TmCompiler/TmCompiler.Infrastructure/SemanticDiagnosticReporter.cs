using System.Text;
using TmCompiler.Domain.Diagnostics;

namespace TmCompiler.Infrastructure;

/// <summary>
/// Presenta los diagnósticos del análisis semántico. No reimplementa el
/// formato: delega en <see cref="Diagnostic.ToStandardFormat"/> para que exista
/// un único bloque de error en todo el compilador.
/// </summary>

public static class SemanticDiagnosticReporter
{
    public static string GenerateDiagnosticReport(
        IEnumerable<Diagnostic> diagnostics,
        string fileName)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        var report = new StringBuilder();
        report.AppendLine($"=== Reporte Semántico: {fileName} ===");
        report.AppendLine(new string('-', 65));

        int errors = 0;
        int warnings = 0;

        foreach (Diagnostic diagnostic in diagnostics)
        {
            if (diagnostic.IsError)
            {
                errors++;
            }
            else
            {
                warnings++;
            }

            report.AppendLine(diagnostic.ToStandardFormat());
        }

        if (errors == 0 && warnings == 0)
        {
            report.AppendLine("Sin diagnósticos.");
            return report.ToString();
        }

        report.AppendLine($"{errors} error(es), {warnings} advertencia(s).");
        return report.ToString();
    }
}
