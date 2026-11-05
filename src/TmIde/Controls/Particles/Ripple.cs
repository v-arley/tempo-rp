using System;
using System.Numerics;

namespace TmIde.Controls.Particles;

/// <summary>
/// Un oleaje que recorre la superficie de la nube: cuánto la levanta
/// (<see cref="Depth"/>), cuántos lóbulos forma alrededor (<see cref="Frequency"/>)
/// y con qué rapidez se desplaza (<see cref="Speed"/>). Son tres ondas cruzadas
/// que al sumarse forman lóbulos y pliegues sin repetirse a simple vista, sin
/// necesidad de una función de ruido.
/// </summary>

public readonly record struct Ripple(float Depth, float Frequency, float Speed)
{
    /// <summary>Superficie lisa.</summary>
    public static Ripple Calm => default;

    /// <summary>Cuánto se aleja la superficie del radio 1 en <paramref name="direction"/>.</summary>
    public float HeightAt(Vector3 direction, float seconds)
    {
        if (Depth == 0)
        {
            return 0;
        }

        var travel = Speed * seconds;
        var first = MathF.Sin(Frequency * direction.X + travel);
        var second = MathF.Sin(Frequency * 1.3f * direction.Y - 0.8f * travel + 1.7f);
        var third = MathF.Sin(Frequency * 0.9f * (direction.Z + direction.X) + 1.2f * travel + 4.1f);

        return Depth * (first + second + third) / 3;
    }
}
