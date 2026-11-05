namespace Tempo.Language.Evaluation;

/// <summary>
/// Destino de <c>pulse</c> sin efecto visible, para los hosts que no tienen
/// panel de partículas (la línea de comandos).
/// </summary>
public sealed class NullRuntimePulse : IRuntimePulse
{
    public static NullRuntimePulse Instance { get; } = new();

    private NullRuntimePulse()
    {
    }

    public void Emit(double intensity)
    {
    }
}
