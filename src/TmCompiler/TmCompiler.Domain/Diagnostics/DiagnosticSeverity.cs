namespace TmCompiler.Domain.Diagnostics;

/// <summary>
/// Severidad de un diagnóstico del compilador, según la Sección 7.1
/// de la especificación TM v1.1.
/// </summary>

public enum DiagnosticSeverity
{
    /// <summary>Error de compilación que impide la ejecución.</summary>
    Error,

    /// <summary>Advertencia que no impide la ejecución.</summary>
    Warning
}
