using System;
using System.Linq;

using TmIde.Controls.Particles;

using Xunit;

namespace TmIde.Tests.Controls.Particles;

/// <summary>
/// Ráfagas de <c>pulse</c> en la simulación: cuantizadas a corcheas del tempo,
/// con tamaño proporcional a la intensidad y vida limitada.
/// </summary>

public sealed class ParticleSimulationTests
{
    private const int Bpm = 120;

    private const double EighthNote = 60.0 / Bpm / BeatClock.SubdivisionsPerBeat;

    private readonly ParticleSimulation _simulation = new(pointCount: 10, new Random(7)) { TempoBpm = Bpm };

    [Fact]
    public void Pulse_WaitsForNextEighthNote()
    {
        _simulation.Pulse(50);
        _simulation.Advance(EighthNote / 4);

        Assert.Equal(0, _simulation.SparkCount);
        Assert.Equal(1, _simulation.WaitingBursts);

        _simulation.Advance(EighthNote);

        Assert.Equal(0, _simulation.WaitingBursts);
        Assert.True(_simulation.SparkCount > 0);
    }

    [Fact]
    public void ThreePulsesAtOnce_SoundOnePerEighthNote_LikeAnArpeggio()
    {
        _simulation.Pulse(40);
        _simulation.Pulse(44);
        _simulation.Pulse(47);

        _simulation.Advance(EighthNote * 1.01);
        Assert.Equal(2, _simulation.WaitingBursts);

        _simulation.Advance(EighthNote);
        Assert.Equal(1, _simulation.WaitingBursts);

        _simulation.Advance(EighthNote);
        Assert.Equal(0, _simulation.WaitingBursts);
    }

    [Theory]
    [InlineData(100, ParticleSimulation.MaxSparksPerBurst)]
    [InlineData(50, ParticleSimulation.MaxSparksPerBurst / 2)]
    [InlineData(0.1, 1)]
    public void Burst_SizeIsProportionalToIntensity(double intensity, int expectedSparks)
    {
        _simulation.Pulse(intensity);
        _simulation.Advance(EighthNote * 1.01);

        Assert.Equal(expectedSparks, _simulation.SparkCount);
    }

    [Fact]
    public void Sparks_FadeAfterTheirLifetime()
    {
        _simulation.Pulse(100);
        _simulation.Advance(EighthNote * 1.01);

        for (var elapsed = 0.0; elapsed <= Spark.Lifetime + 0.1; elapsed += 0.05)
        {
            _simulation.Advance(0.05);
        }

        Assert.Equal(0, _simulation.SparkCount);
    }

    [Fact]
    public void WaitingBursts_AreCapped()
    {
        foreach (var _ in Enumerable.Range(0, ParticleSimulation.MaxWaitingBursts + 10))
        {
            _simulation.Pulse(10);
        }

        Assert.Equal(ParticleSimulation.MaxWaitingBursts, _simulation.WaitingBursts);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.5)]
    [InlineData(1.3)]
    public void Correct_KeepsTheSameSizeOnEveryBeat(double seconds)
    {
        _simulation.Advance(seconds);

        Assert.All(_simulation.Points(), point => Assert.Equal(1f, point.Length(), precision: 4));
    }

    [Theory]
    [InlineData(DiagnosticState.Correct)]
    [InlineData(DiagnosticState.Warning)]
    [InlineData(DiagnosticState.Error)]
    public void Points_IncludeWholeSurfaceInEveryState(DiagnosticState state)
    {
        _simulation.Mood = new DiagnosticMood(state, 1);
        _simulation.Advance(0.5);

        Assert.Equal(10, _simulation.Points().Count());
    }
}
