using System;
using System.Collections.Generic;

namespace TmIde.Controls.Particles;

/// <summary>Color de un punto, en componentes de 0 a 255.</summary>

public readonly record struct DotColor(byte R, byte G, byte B)
{
    /// <summary>Mezcla con <paramref name="other"/>: 0 da este color, 1 da el otro.</summary>
    public DotColor Toward(DotColor other, float amount)
    {
        var t = Math.Clamp(amount, 0f, 1f);

        return new DotColor(Mix(R, other.R, t), Mix(G, other.G, t), Mix(B, other.B, t));
    }

    /// <summary>Promedio de colores pesado; los pesos deben sumar 1.</summary>
    public static DotColor Blend(IEnumerable<(DotColor Color, float Weight)> parts)
    {
        float r = 0, g = 0, b = 0;

        foreach (var (color, weight) in parts)
        {
            r += color.R * weight;
            g += color.G * weight;
            b += color.B * weight;
        }

        return new DotColor(ToByte(r), ToByte(g), ToByte(b));
    }

    private static byte Mix(byte from, byte to, float t) => (byte)MathF.Round(from + (to - from) * t);

    private static byte ToByte(float channel) => (byte)Math.Clamp(MathF.Round(channel), 0, 255);
}
