using System;
using System.Linq;

using TmIde.Controls.Particles;

using Xunit;

namespace TmIde.Tests.Controls.Particles;

/// <summary>Esfera de Fibonacci y forma de la nube en cada estado de diagnóstico.</summary>

public sealed class SurfaceShapeTests
{
    private readonly System.Numerics.Vector3[] _sphere = FibonacciSphere.Points(1000);

    [Fact]
    public void FibonacciSphere_PointsLieOnUnitSphere()
    {
        Assert.All(_sphere, direction => Assert.Equal(1f, direction.Length(), precision: 4));
    }

    [Fact]
    public void FibonacciSphere_IsEvenlySplitBetweenHemispheres()
    {
        Assert.InRange(_sphere.Count(direction => direction.Y > 0), 490, 510);
    }

    [Fact]
    public void Correct_IsAPerfectSphere()
    {
        var correct = SurfaceShape.For(DiagnosticState.Correct, turbulence: 1);

        Assert.Same(SurfaceShape.Sphere, correct);
        Assert.All(_sphere, direction => Assert.Equal(1f, correct.RadiusAt(direction, 2.5f, new Random(3))));
    }

    [Theory]
    [InlineData(DiagnosticState.Warning, 0.35f)]
    [InlineData(DiagnosticState.Error, 0.65f)]
    public void Shapes_StayCloseToTheSphere(DiagnosticState state, float maxDeformation)
    {
        var shape = SurfaceShape.For(state, turbulence: 1);
        var random = new Random(3);

        Assert.All(_sphere, direction => Assert.InRange(shape.RadiusAt(direction, 2.5f, random), 1 - maxDeformation, 1 + maxDeformation));
    }

    [Fact]
    public void MoreProblems_DeformMore()
    {
        float Spread(DiagnosticState state, float turbulence)
        {
            var shape = SurfaceShape.For(state, turbulence);
            var radii = _sphere.Select(direction => shape.RadiusAt(direction, 2.5f, new Random(3))).ToList();

            return radii.Max() - radii.Min();
        }

        Assert.True(Spread(DiagnosticState.Error, 1) > Spread(DiagnosticState.Error, 0.1f));
        Assert.True(Spread(DiagnosticState.Warning, 1) > Spread(DiagnosticState.Warning, 0.1f));
        Assert.True(Spread(DiagnosticState.Error, 1) > Spread(DiagnosticState.Warning, 1));
    }

    [Fact]
    public void CalmRipple_LeavesTheSurfaceFlat()
    {
        Assert.Equal(0f, Ripple.Calm.HeightAt(_sphere[0], 10));
    }
}
