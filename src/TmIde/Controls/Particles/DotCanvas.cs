using System;

namespace TmIde.Controls.Particles;

/// <summary>
/// Lienzo de píxeles del panel, sobre el fondo del tema activo
/// (<see cref="Background"/>). Cada punto tiñe el píxel hacia su color como una
/// gota de tinta: donde la nube se cruza consigo misma el tono se intensifica.
/// Los píxeles están en el formato <c>Bgr32</c> que acepta un <c>WriteableBitmap</c>.
/// </summary>

public sealed class DotCanvas
{
    /// <summary>Fondo mientras nadie indique otro: la superficie del tema claro, #A4B1C7.</summary>
    public static DotColor DefaultBackground { get; } = new(0xA4, 0xB1, 0xC7);

    public DotCanvas(int width, int height)
    {
        Width = Math.Max(0, width);
        Height = Math.Max(0, height);
        Pixels = new int[Width * Height];
        Clear();
    }

    public int Width { get; }

    public int Height { get; }

    public int[] Pixels { get; }

    /// <summary>Color con que <see cref="Clear"/> rellena el lienzo; sigue al tema.</summary>
    public DotColor Background { get; set; } = DefaultBackground;

    /// <summary>Bytes por fila, como los pide <c>WriteableBitmap.WritePixels</c>.</summary>
    public int Stride => Width * sizeof(int);

    public void Clear() => Array.Fill(Pixels, Pack(Background));

    /// <summary>Tiñe el píxel hacia <paramref name="color"/>, con <paramref name="opacity"/> de 0 a 1.</summary>
    public void Ink(int x, int y, DotColor color, float opacity)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
        {
            return;
        }

        Pixels[y * Width + x] = Pack(ColorAt(x, y).Toward(color, opacity));
    }

    public DotColor ColorAt(int x, int y)
    {
        var pixel = Pixels[y * Width + x];

        return new DotColor((byte)(pixel >> 16), (byte)(pixel >> 8), (byte)pixel);
    }

    private static int Pack(DotColor color) =>
        unchecked((int)0xFF000000) | (color.R << 16) | (color.G << 8) | color.B;
}
