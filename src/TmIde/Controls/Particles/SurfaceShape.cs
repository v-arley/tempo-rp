using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Numerics;

namespace TmIde.Controls.Particles;

/// <summary>
/// Forma de la nube: lóbulos grandes, pliegues finos encima y un temblor al
/// azar. Cada estado la define en una fila de <see cref="ByState"/>, la tabla
/// de la ___ escrita como datos, en función de la turbulencia (de 0 a 1: más
/// problemas, más distorsión y turbulencia).
/// </summary>

public sealed record SurfaceShape(Ripple Lobes, Ripple Folds, float Tremor)
{
    /// <summary>Esfera perfecta.</summary>
    public static SurfaceShape Sphere { get; } = new(Ripple.Calm, Ripple.Calm, Tremor: 0);

    private static readonly FrozenDictionary<DiagnosticState, Func<float, SurfaceShape>> ByState =
        new Dictionary<DiagnosticState, Func<float, SurfaceShape>>
        {
            // Correcto: en calma.
            [DiagnosticState.Correct] = _ => Sphere,

            // Advertencia: lóbulos que ondulan; con más advertencias se hunden,
            // se aceleran y empiezan a temblar, sin llegar a la turbulencia de Error.
            [DiagnosticState.Warning] = turbulence => new(
                Lobes: new(Depth: 0.12f + 0.12f * turbulence, Frequency: 3f, Speed: 1.1f * (1 + 0.6f * turbulence)),
                Folds: new(Depth: 0.04f + 0.03f * turbulence, Frequency: 6f, Speed: 1.7f * (1 + 0.6f * turbulence)),
                Tremor: 0.04f * turbulence),

            // Error: se retuerce más hondo, más rápido y tiembla con más errores.
            [DiagnosticState.Error] = turbulence => new(
                Lobes: new(Depth: 0.22f + 0.18f * turbulence, Frequency: 3.2f, Speed: 1.8f * (1 + turbulence)),
                Folds: new(Depth: 0.1f, Frequency: 7f, Speed: 2.6f * (1 + turbulence)),
                Tremor: 0.08f * turbulence),
        }.ToFrozenDictionary();

    public static SurfaceShape For(DiagnosticState state, float turbulence) => ByState[state](turbulence);

    /// <summary>Radio de la superficie en <paramref name="direction"/>; 1 es la esfera en reposo.</summary>
    public float RadiusAt(Vector3 direction, float seconds, Random random) =>
        1 + Lobes.HeightAt(direction, seconds) + Folds.HeightAt(direction, seconds) + TremorFrom(random);

    private float TremorFrom(Random random) => Tremor == 0 ? 0 : Tremor * ((float)random.NextDouble() - 0.5f);
}
