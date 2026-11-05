using System;
using System.Collections.Generic;
using System.Numerics;

namespace TmIde.Controls.Particles;

/// <summary>
/// Dibuja puntos 3D como gotas de tinta sobre un <see cref="DotCanvas"/>
/// : proyección en perspectiva, y cuanto más cerca de la cámara está un
/// punto, más grande y más intenso se ve. Cada punto se sombrea con una luz
/// fija, como un dibujo a lápiz de una esfera: la cara iluminada toma el tono
/// <see cref="DotPalette.Lit"/> y la opuesta, <see cref="DotPalette.Shadow"/>.
/// </summary>

public static class DotRenderer
{
    /// <summary>Distancia de la cámara al centro de la escena.</summary>
    public const float CameraDistance = 3.2f;

    /// <summary>Profundidad de la escena a cada lado del centro.</summary>
    public const float SceneDepth = 1.6f;

    /// <summary>Radio de la escena que debe caber en el panel.</summary>
    public const float SceneRadius = 1.5f;

    /// <summary>Parte del panel que ocupa la escena; el resto es margen.</summary>
    public const float FillRatio = 0.85f;

    // A partir de esta cercanía un punto ocupa 2×2 píxeles en lugar de uno.
    private const float LargeDotCloseness = 0.55f;

    // Píxeles que ocupa un punto, relativos a su posición.
    private static readonly (int X, int Y)[] SmallDot = [(0, 0)];
    private static readonly (int X, int Y)[] LargeDot = [(0, 0), (1, 0), (0, 1), (1, 1)];

    // Opacidad del punto más lejano; el más cercano se pinta opaco.
    private const float FarthestOpacity = 0.2f;

    // La luz llega desde arriba a la izquierda y algo de frente: la cara visible
    // queda blanca arriba a la izquierda y negra abajo a la derecha.
    private static readonly Vector3 TowardLight = Vector3.Normalize(new Vector3(-0.7f, 0.5f, -0.4f));

    // Cuánto se estrecha la franja entre luz y sombra: con más contraste quedan
    // menos puntos de tono medio, que se confundirían con el fondo.
    private const float ShadingContrast = 2.4f;

    public static void Render(IEnumerable<Vector3> points, DotCanvas canvas, DotPalette palette)
    {
        canvas.Clear();

        if (canvas.Width == 0 || canvas.Height == 0)
        {
            return;
        }

        // Un punto a SceneRadius del centro, a la distancia de la cámara, llega
        // justo al borde de la zona que ocupa la escena.
        var focal = Math.Min(canvas.Width, canvas.Height) * FillRatio / 2 * CameraDistance / SceneRadius;

        foreach (var point in points)
        {
            var distance = point.Z + CameraDistance;
            if (distance <= 0.1f)
            {
                continue;
            }

            var x = (int)MathF.Round(canvas.Width / 2f + point.X / distance * focal);
            var y = (int)MathF.Round(canvas.Height / 2f - point.Y / distance * focal);
            var closeness = ClosenessAt(distance);
            var color = palette.At(ShadowAt(point));
            var opacity = FarthestOpacity + (1 - FarthestOpacity) * closeness;

            foreach (var (dx, dy) in closeness >= LargeDotCloseness ? LargeDot : SmallDot)
            {
                canvas.Ink(x + dx, y + dy, color, opacity);
            }
        }
    }

    /// <summary>
    /// Sombra de un punto, de 0 (de cara a la luz) a 1 (de espaldas). La nube
    /// está centrada en el origen, así que la dirección del punto hace de normal;
    /// el centro mismo, donde nacen las chispas, queda a media luz.
    /// </summary>
    public static float ShadowAt(Vector3 point) => point == Vector3.Zero
        ? 0.5f
        : Math.Clamp(0.5f - Vector3.Dot(Vector3.Normalize(point), TowardLight) * ShadingContrast / 2, 0f, 1f);

    /// <summary>Cercanía a la cámara de un punto a <paramref name="distance"/>: 1 delante, 0 al fondo.</summary>
    public static float ClosenessAt(float distance) =>
        Math.Clamp((CameraDistance + SceneDepth - distance) / (2 * SceneDepth), 0f, 1f);
}
