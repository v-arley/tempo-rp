namespace Tempo.Language.Binding;

/// <summary>
/// Límites de la declaración <c>tempo</c>, en BPM: el rango admitido y el
/// ritmo que se usa cuando el proyecto no declara ninguno.
/// </summary>

public static class TempoLimits
{
    public const int MinBpm = 64;
    public const int MaxBpm = 300;
    public const int DefaultBpm = 128;

    /// <summary>
    /// Convierte el lexema de la declaración en BPM. Devuelve <c>false</c> si
    /// queda fuera de <see cref="MinBpm"/>–<see cref="MaxBpm"/> o no cabe en un entero.
    /// </summary>
    public static bool TryParse(string value, out int bpm)
    {
        return int.TryParse(value, out bpm) && bpm >= MinBpm && bpm <= MaxBpm;
    }
}
