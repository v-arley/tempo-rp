using System;
using System.Linq;

using TmIde.Controls.Particles;

using Xunit;

namespace TmIde.Tests.Controls.Particles;

/// <summary>Fundido entre estados del proyecto: forma y color cambian sin saltos.</summary>

public sealed class MoodMixerTests
{
    private static readonly System.Numerics.Vector3[] Sphere = FibonacciSphere.Points(200);

    private readonly MoodMixer _mixer = new();

    [Fact]
    public void StartsFullyCorrect()
    {
        Assert.Equal(1f, _mixer.LevelOf(DiagnosticState.Correct));
        Assert.Equal(DotPalette.For(DiagnosticState.Correct), _mixer.Palette);
    }

    [Fact]
    public void NewTarget_FadesInGradually_NotAtOnce()
    {
        _mixer.Target = DiagnosticMood.From(errors: 0, warnings: 3);

        _mixer.Advance(MoodMixer.FadeSeconds / 3);
        var early = _mixer.LevelOf(DiagnosticState.Warning);

        _mixer.Advance(MoodMixer.FadeSeconds);
        var later = _mixer.LevelOf(DiagnosticState.Warning);

        Assert.InRange(early, 0.05f, 0.5f);
        Assert.True(later > early);
        Assert.True(_mixer.LevelOf(DiagnosticState.Correct) < 1);
    }

    [Fact]
    public void AfterSeveralFadeTimes_TargetIsAllThatIsHeard()
    {
        _mixer.Target = DiagnosticMood.From(errors: 2, warnings: 0);

        // Tres segundos a 30 fotogramas por segundo.
        for (var frame = 0; frame < 90; frame++)
        {
            _mixer.Advance(0.033f);
        }

        var voice = Assert.Single(_mixer.Voices);
        Assert.Equal(DiagnosticState.Error, voice.State);
        Assert.Equal(DotPalette.For(DiagnosticState.Error), _mixer.Palette);
    }

    [Fact]
    public void MidTransition_ShapeLiesBetweenBothStates()
    {
        _mixer.Target = DiagnosticMood.From(errors: 10, warnings: 0);
        _mixer.Advance(MoodMixer.FadeSeconds);

        var deformations = Sphere.Select(direction => MathF.Abs(_mixer.RadiusAt(direction, 2.5f, new Random(1)) - 1)).ToList();

        Assert.True(deformations.Max() > 0.01f, "Ya no es la esfera perfecta");
        Assert.True(deformations.Max() < 0.6f, "Todavía no es el error completo");
    }

    [Fact]
    public void MidTransition_PaletteLiesBetweenBothStates()
    {
        _mixer.Target = DiagnosticMood.From(errors: 0, warnings: 1);
        _mixer.Advance(MoodMixer.FadeSeconds);

        var (from, to, now) = (DotPalette.For(DiagnosticState.Correct).Lit, DotPalette.For(DiagnosticState.Warning).Lit, _mixer.Palette.Lit);

        Assert.InRange(now.G, Math.Min(from.G, to.G), Math.Max(from.G, to.G));
        Assert.NotEqual(from, now);
        Assert.NotEqual(to, now);
    }

    [Fact]
    public void Voices_AlwaysWeighOne()
    {
        _mixer.Target = DiagnosticMood.From(errors: 0, warnings: 2);
        _mixer.Advance(0.2f);
        _mixer.Target = DiagnosticMood.From(errors: 4, warnings: 0);
        _mixer.Advance(0.2f);

        Assert.Equal(1f, _mixer.Voices.Sum(voice => voice.Weight), precision: 4);
        Assert.Equal(3, _mixer.Voices.Count);
    }
}
