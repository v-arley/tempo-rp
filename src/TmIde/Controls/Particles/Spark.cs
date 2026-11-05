using System;
using System.Numerics;

namespace TmIde.Controls.Particles;

/// <summary>Partícula de una ráfaga de <c>pulse</c>: sale del centro y se apaga.</summary>

public readonly record struct Spark(Vector3 Position, Vector3 Velocity, float Age)
{
    /// <summary>Segundos que vive una chispa.</summary>
    public const float Lifetime = 1.4f;

    // Frenado por segundo: la ráfaga se abre rápido y se queda flotando.
    private const float Drag = 1.6f;

    public bool IsAlive => Age < Lifetime;

    public static Spark FromCenter(Vector3 velocity) => new(Vector3.Zero, velocity, Age: 0);

    public Spark After(float seconds) => new(
        Position + Velocity * seconds,
        Velocity * MathF.Max(0, 1 - Drag * seconds),
        Age + seconds);
}
