using System;

namespace TmIde.Controls.Particles;

/// <summary>
/// Metrónomo de la animación: cuenta pulsos al ritmo de <c>tempo</c>
/// y avisa de cada corchea, el instante en que puede sonar una ráfaga.
/// </summary>

public sealed class BeatClock
{
    /// <summary>Las ráfagas se cuantizan a corcheas: dos por pulso.</summary>
    public const int SubdivisionsPerBeat = 2;

    private double _beats;

    public float Seconds { get; private set; }

    /// <summary>Avanza el reloj y devuelve cuántas corcheas empezaron en ese lapso.</summary>
    public int Advance(double seconds, int beatsPerMinute)
    {
        var subdivisionsBefore = Math.Floor(_beats * SubdivisionsPerBeat);

        _beats += seconds * beatsPerMinute / 60;
        Seconds += (float)seconds;

        return (int)(Math.Floor(_beats * SubdivisionsPerBeat) - subdivisionsBefore);
    }
}
