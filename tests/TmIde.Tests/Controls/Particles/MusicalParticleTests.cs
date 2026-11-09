
using System;
using System.Linq;

using TmIde.Controls.Particles;

using Xunit;

namespace TmIde.Tests.Controls.Particles;

public sealed class MusicalParticleTests
{
    [Fact]
    public void WithoutMusic_CorrectStateRemainsSphere()
    {
        var simulation = new ParticleSimulation(
            pointCount: 200,
            random: new Random(5));

        simulation.Advance(0.1);

        Assert.All(
            simulation.Points(),
            point => Assert.Equal(
                1f,
                point.Length(),
                precision: 4));
    }

    [Fact]
    public void MusicalNote_DeformsSurface()
    {
        var simulation = new ParticleSimulation(
            pointCount: 200,
            random: new Random(5));

        simulation.MusicalNoteOn(
            midiNote: 60,
            sustained: false);

        simulation.Advance(0.12);

        var radii = simulation.Points()
            .Select(point => point.Length())
            .ToArray();

        Assert.Contains(
            radii,
            radius => Math.Abs(radius - 1f) > 0.01f);
    }

    [Fact]
    public void ShortNote_DisappearsAndRestoresSphere()
    {
        var simulation = new ParticleSimulation(
            pointCount: 100);

        simulation.MusicalNoteOn(64, false);
        simulation.Advance(0.1);

        Assert.Equal(
            1,
            simulation.ActiveMusicalWaves);

        simulation.Advance(1.5);

        Assert.Equal(
            0,
            simulation.ActiveMusicalWaves);

        Assert.All(
            simulation.Points(),
            point => Assert.Equal(
                1f,
                point.Length(),
                precision: 4));
    }

    [Fact]
    public void SustainedNote_RemainsUntilReleased()
    {
        var simulation = new ParticleSimulation(
            pointCount: 100);

        simulation.MusicalNoteOn(69, true);

        simulation.Advance(3.0);

        Assert.Equal(
            1,
            simulation.ActiveMusicalWaves);

        simulation.MusicalNoteOff(69);
        simulation.Advance(0.9);

        Assert.Equal(
            0,
            simulation.ActiveMusicalWaves);
    }

    [Fact]
    public void Music_DoesNotChangeWarningPalette()
    {
        var withMusic = new ParticleSimulation(
            pointCount: 100);

        var withoutMusic = new ParticleSimulation(
            pointCount: 100);

        var warning = DiagnosticMood.From(
            errors: 0,
            warnings: 3);

        withMusic.Mood = warning;
        withoutMusic.Mood = warning;

        withMusic.Advance(0.5);
        withoutMusic.Advance(0.5);

        withMusic.MusicalNoteOn(72, false);

        withMusic.Advance(0.1);
        withoutMusic.Advance(0.1);

        Assert.Equal(
            withoutMusic.Palette,
            withMusic.Palette);

        Assert.Equal(
            warning,
            withMusic.Mood);
    }

    [Fact]
    public void Music_DoesNotReplaceErrorState()
    {
        var simulation = new ParticleSimulation(
            pointCount: 100);

        var error = DiagnosticMood.From(
            errors: 4,
            warnings: 0);

        simulation.Mood = error;

        simulation.MusicalNoteOn(60, false);
        simulation.Advance(0.2);

        Assert.Equal(
            error,
            simulation.Mood);

        simulation.StopMusicalNotes();

        Assert.Equal(
            error,
            simulation.Mood);
    }

    [Fact]
    public void Music_DoesNotChangePulseScheduling()
    {
        var simulation = new ParticleSimulation(
            pointCount: 100)
        {
            TempoBpm = 120
        };

        simulation.Pulse(50);
        simulation.MusicalNoteOn(64, false);

        simulation.Advance(0.02);

        Assert.Equal(1, simulation.WaitingBursts);
        Assert.Equal(0, simulation.SparkCount);

        // Superar la siguiente corchea del BeatClock.
        simulation.Advance(0.30);

        Assert.Equal(0, simulation.WaitingBursts);
        Assert.True(simulation.SparkCount > 0);
        Assert.True(simulation.ActiveMusicalWaves > 0);
    }

    [Fact]
    public void TooManyNotes_AreLimited()
    {
        var simulation = new ParticleSimulation(
            pointCount: 100);

        for (var i = 0; i < 50; i++)
        {
            simulation.MusicalNoteOn(
                48 + i,
                sustained: false);
        }

        Assert.Equal(
            ParticleSimulation.MaxMusicalWaves,
            simulation.ActiveMusicalWaves);
    }

    [Fact]
    public void DisablingMusic_DoesNotClearPulseOrMood()
    {
        var simulation = new ParticleSimulation(
            pointCount: 100);

        var warning = DiagnosticMood.From(
            errors: 0,
            warnings: 2);

        simulation.Mood = warning;

        simulation.Pulse(70);
        simulation.MusicalNoteOn(60, true);

        simulation.StopMusicalNotes();

        Assert.Equal(
            0,
            simulation.ActiveMusicalWaves);

        Assert.Equal(
            1,
            simulation.WaitingBursts);

        Assert.Equal(
            warning,
            simulation.Mood);
    }
}
