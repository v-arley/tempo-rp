using TmIde.Controls.Particles;

using Xunit;

namespace TmIde.Tests.Controls.Particles;

/// <summary>Tabla de estados/summary>

public sealed class DiagnosticMoodTests
{
    [Theory]
    [InlineData(0, 0, DiagnosticState.Correct)]
    [InlineData(0, 1, DiagnosticState.Warning)]
    [InlineData(0, 7, DiagnosticState.Warning)]
    [InlineData(1, 0, DiagnosticState.Error)]
    [InlineData(3, 5, DiagnosticState.Error)]
    public void From_Counts_FollowsDiagnosticTable(int errors, int warnings, DiagnosticState expected)
    {
        Assert.Equal(expected, DiagnosticMood.From(errors, warnings).State);
    }

    [Theory]
    [InlineData(1, 0.1)]
    [InlineData(5, 0.5)]
    [InlineData(10, 1.0)]
    [InlineData(25, 1.0)]
    public void Error_TurbulenceGrowsWithErrors_UpToTen(int errors, double expected)
    {
        Assert.Equal(expected, DiagnosticMood.From(errors, 0).Turbulence, precision: 6);
    }

    [Theory]
    [InlineData(1, 0.1)]
    [InlineData(5, 0.5)]
    [InlineData(10, 1.0)]
    [InlineData(25, 1.0)]
    public void Warning_TurbulenceGrowsWithWarnings_UpToTen(int warnings, double expected)
    {
        Assert.Equal(expected, DiagnosticMood.From(0, warnings).Turbulence, precision: 6);
    }

    [Fact]
    public void Error_IgnoresWarnings_WhenMeasuringTurbulence()
    {
        Assert.Equal(DiagnosticMood.From(2, 0), DiagnosticMood.From(2, 9));
    }

    [Fact]
    public void Correct_IsCorrectWithoutTurbulence()
    {
        Assert.Equal(new DiagnosticMood(DiagnosticState.Correct, 0), DiagnosticMood.From(0, 0));
    }
}
