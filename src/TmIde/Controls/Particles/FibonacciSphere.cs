using System;
using System.Linq;
using System.Numerics;

namespace TmIde.Controls.Particles;

/// <summary>
/// Puntos repartidos por igual sobre una esfera de radio 1 con la espiral de
/// Fibonacci (como las semillas de un girasol): sin polos amontonados y con un
/// dibujo de malla que se nota al girar.
/// </summary>

public static class FibonacciSphere
{
    // Ángulo áureo: cada punto gira esto respecto al anterior.
    private static readonly float GoldenAngle = MathF.PI * (3 - MathF.Sqrt(5));

    /// <summary>Direcciones unitarias de <paramref name="count"/> puntos.</summary>
    public static Vector3[] Points(int count) =>
        Enumerable.Range(0, count).Select(index => OnSpiral(index, count)).ToArray();

    private static Vector3 OnSpiral(int index, int count)
    {
        var height = 1 - 2 * (index + 0.5f) / count;
        var radius = MathF.Sqrt(1 - height * height);
        var turn = GoldenAngle * index;

        return new Vector3(radius * MathF.Cos(turn), height, radius * MathF.Sin(turn));
    }
}
