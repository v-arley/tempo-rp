using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace TmIde.Controls.Particles;

/// <summary>
/// Mesa de mezclas de los estados del proyecto: cada estado es un canal con su
/// volumen. Al cambiar el <see cref="Target"/>, el canal nuevo sube y los
/// demás bajan poco a poco, así que la forma y el color de la nube se funden
/// de un estado a otro en lugar de saltar. Si el objetivo cambia a mitad de
/// una transición, los volúmenes siguen desde donde estaban: nunca hay saltos.
/// </summary>

public sealed class MoodMixer
{
    /// <summary>
    /// Constante de tiempo del fundido: en este lapso un canal recorre unos dos
    /// tercios del camino, y en tres veces este lapso, casi todo.
    /// </summary>
    public const float FadeSeconds = 0.45f;

    // Por debajo de este volumen un canal no se calcula: no se nota.
    private const float SilentLevel = 0.01f;

    private static readonly DiagnosticState[] Channels = Enum.GetValues<DiagnosticState>();

    private readonly float[] _levels = Channels.Select(state => state == DiagnosticState.Correct ? 1f : 0f).ToArray();
    private MixerVoice[] _voices = [new(DiagnosticState.Correct, 1, SurfaceShape.Sphere)];
    private float _turbulence;

    /// <summary>Estado hacia el que se funde la mezcla.</summary>
    public DiagnosticMood Target { get; set; } = DiagnosticMood.Correct;

    /// <summary>Canales que se oyen en este fotograma, con pesos que suman 1.</summary>
    public IReadOnlyList<MixerVoice> Voices => _voices;

    /// <summary>Paleta resultante: el promedio de las paletas de las voces, pesado.</summary>
    public DotPalette Palette => DotPalette.Blend(_voices.Select(voice => (DotPalette.For(voice.State), voice.Weight)).ToList());

    /// <summary>Volumen del canal de <paramref name="state"/>, de 0 a 1.</summary>
    public float LevelOf(DiagnosticState state) => _levels[(int)state];

    /// <summary>
    /// Acerca cada canal a su volumen objetivo (1 el del estado actual, 0 los
    /// demás) y fija la forma de cada voz audible para este fotograma.
    /// </summary>
    public void Advance(float seconds)
    {
        var approach = 1 - MathF.Exp(-seconds / FadeSeconds);

        foreach (var state in Channels)
        {
            _levels[(int)state] += (GoalOf(state) - LevelOf(state)) * approach;
        }

        _turbulence += ((float)Target.Turbulence - _turbulence) * approach;

        var audible = Channels.Where(state => LevelOf(state) > SilentLevel).ToArray();
        var total = audible.Sum(LevelOf);

        _voices = audible
            .Select(state => new MixerVoice(state, LevelOf(state) / total, SurfaceShape.For(state, _turbulence)))
            .ToArray();
    }

    /// <summary>Radio de la superficie: el de cada voz, pesado por su volumen.</summary>
    public float RadiusAt(Vector3 direction, float seconds, Random random) =>
        _voices.Sum(voice => voice.Weight * voice.Shape.RadiusAt(direction, seconds, random));

    private float GoalOf(DiagnosticState state) => state == Target.State ? 1 : 0;
}

/// <summary>Un canal audible de la mezcla: su estado, su peso y su forma en este fotograma.</summary>
public readonly record struct MixerVoice(DiagnosticState State, float Weight, SurfaceShape Shape);
