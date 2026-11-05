using System.Linq;
using System.Numerics;

using TmIde.Controls.Particles;

using Xunit;

namespace TmIde.Tests.Controls.Particles;

/// <summary>
/// Proyección en perspectiva, intensidad por cercanía y degradado vertical de
/// la nube de puntos sobre el fondo del panel.
/// </summary>

public sealed class DotRendererTests
{
    private static readonly DotPalette Correct = DotPalette.For(DiagnosticState.Correct);
    private static readonly DotColor Background = DotCanvas.DefaultBackground;

    // Superficie del tema oscuro (SurfaceBackgroundBrush de Theme.Dark.xaml).
    private static readonly DotColor DarkSurface = new(0x26, 0x2D, 0x36);

    private readonly DotCanvas _canvas = new(width: 100, height: 100);

    [Fact]
    public void Render_NoPoints_LeavesOnlyBackground()
    {
        DotRenderer.Render([], _canvas, Correct);

        Assert.Equal(0, InkedPixels(_canvas));
        Assert.Equal(new DotColor(0xA4, 0xB1, 0xC7), _canvas.ColorAt(0, 0));
    }

    [Fact]
    public void Render_PointAtCenter_InksCenterPixel()
    {
        DotRenderer.Render([Vector3.Zero], _canvas, Correct);

        Assert.NotEqual(Background, _canvas.ColorAt(50, 50));
    }

    [Fact]
    public void Render_NearPoint_IsBiggerAndStrongerThanFarPoint()
    {
        var near = new DotCanvas(100, 100);
        var far = new DotCanvas(100, 100);

        DotRenderer.Render([new Vector3(0, 0, -DotRenderer.SceneDepth)], near, Correct);
        DotRenderer.Render([new Vector3(0, 0, DotRenderer.SceneDepth)], far, Correct);

        Assert.True(InkedPixels(near) > InkedPixels(far));
        Assert.True(DistanceFromBackground(near.ColorAt(50, 50)) > DistanceFromBackground(far.ColorAt(50, 50)));
    }

    [Fact]
    public void LitSide_TakesLitTone_ShadowSide_TakesShadowTone()
    {
        // La luz viene de arriba: la cara superior se ilumina y la inferior queda en sombra.
        DotRenderer.Render([new Vector3(0, 0.6f, 0), new Vector3(0, -0.6f, 0)], _canvas, Correct);

        var top = FirstInkedColorFrom(top: true);
        var bottom = FirstInkedColorFrom(top: false);

        Assert.True(top.R > bottom.R, "La cara iluminada es más roja que la de sombra");
        Assert.True(top.G > bottom.G, "y también más clara");
    }

    [Fact]
    public void Canvas_ClearsWithTheThemeBackground()
    {
        _canvas.Background = DarkSurface;
        _canvas.Clear();

        Assert.Equal(DarkSurface, _canvas.ColorAt(0, 0));
    }

    [Theory]
    [InlineData(DiagnosticState.Correct)]
    [InlineData(DiagnosticState.Warning)]
    [InlineData(DiagnosticState.Error)]
    public void ShadowTones_StandOutOnBothThemes(DiagnosticState state)
    {
        // Diferencia mínima de luminosidad (suma de canales) para distinguir un punto.
        const int Legible = 120;
        var shadow = DotPalette.For(state).Shadow;

        Assert.True(Lightness(shadow) - Lightness(DarkSurface) >= Legible, "Se ve sobre el tema oscuro");
        Assert.True(DistanceFromBackground(shadow) >= Legible, "Se ve sobre el tema claro");
    }

    [Fact]
    public void Harmony_SharesDissonanceColors_SoOnlyTheShapeRevealsErrors()
    {
        Assert.Equal(DotPalette.For(DiagnosticState.Error), Correct);
        Assert.NotEqual(Correct, DotPalette.For(DiagnosticState.Warning));
    }

    [Fact]
    public void ShadowAt_FacingLightIsZero_FacingAwayIsOne()
    {
        Assert.Equal(0f, DotRenderer.ShadowAt(new Vector3(-0.7f, 0.5f, -0.4f)));
        Assert.Equal(1f, DotRenderer.ShadowAt(new Vector3(0.7f, -0.5f, 0.4f)));
    }

    [Fact]
    public void Render_PointBehindCamera_IsSkipped()
    {
        DotRenderer.Render([new Vector3(0, 0, -DotRenderer.CameraDistance - 1)], _canvas, Correct);

        Assert.Equal(0, InkedPixels(_canvas));
    }

    [Fact]
    public void Canvas_OverlappingInk_DeepensTowardItsColor()
    {
        var ink = new DotColor(0, 0, 0);

        _canvas.Ink(1, 1, ink, 0.5f);
        var once = _canvas.ColorAt(1, 1);
        _canvas.Ink(1, 1, ink, 0.5f);
        var twice = _canvas.ColorAt(1, 1);

        Assert.True(Lightness(twice) < Lightness(once));
        Assert.True(Lightness(once) < Lightness(Background));
    }

    [Fact]
    public void Canvas_InkOutsideBounds_IsIgnored()
    {
        _canvas.Ink(-1, 500, new DotColor(0, 0, 0), 1);

        Assert.Equal(0, InkedPixels(_canvas));
    }

    private static int InkedPixels(DotCanvas canvas) =>
        Enumerable.Range(0, canvas.Width * canvas.Height)
            .Count(index => canvas.ColorAt(index % canvas.Width, index / canvas.Width) != Background);

    private static int Lightness(DotColor color) => color.R + color.G + color.B;

    private static int DistanceFromBackground(DotColor color) =>
        System.Math.Abs(color.R - Background.R) + System.Math.Abs(color.G - Background.G) + System.Math.Abs(color.B - Background.B);

    private DotColor FirstInkedColorFrom(bool top)
    {
        var rows = Enumerable.Range(0, _canvas.Height);

        foreach (var y in top ? rows : rows.Reverse())
        {
            foreach (var x in Enumerable.Range(0, _canvas.Width))
            {
                if (_canvas.ColorAt(x, y) is var color && color != Background)
                {
                    return color;
                }
            }
        }

        return Background;
    }
}
