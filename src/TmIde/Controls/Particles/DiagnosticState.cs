namespace TmIde.Controls.Particles;

/// <summary>Estado del proyecto según sus diagnósticos, de más sano a más roto.</summary>

public enum DiagnosticState
{
    /// <summary>0 errores y 0 advertencias: órbita ordenada.</summary>
    Correct,

    /// <summary>0 errores y alguna advertencia: distorsión que crece con las advertencias.</summary>
    Warning,

    /// <summary>Algún error: turbulencia que crece con los errores.</summary>
    Error,
}
