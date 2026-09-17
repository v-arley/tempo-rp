using TmCompiler.Domain.Semantic;
using Xunit;

namespace TmCompiler.Tests.Semantic;

public sealed class TypeCompatibilityTests
{
    [Fact]
    public void IsAssignable_SameType_IsTrue()
    {
        Assert.True(TypeCompatibility.IsAssignable(TmType.Sample, TmType.Sample));
        Assert.True(
            TypeCompatibility.IsAssignable(
                TmType.SequenceOf(TmType.Tone),
                TmType.SequenceOf(TmType.Tone)));
    }

    [Fact]
    public void IsAssignable_SampleAndWave_IsFalseInBothDirections()
    {
        // TM no tiene coerción numérica implícita, ni siquiera ampliando (§4.4.1.1).
        Assert.False(TypeCompatibility.IsAssignable(TmType.Sample, TmType.Wave));
        Assert.False(TypeCompatibility.IsAssignable(TmType.Wave, TmType.Sample));
    }

    [Fact]
    public void IsAssignable_SilenceToPhrase_IsTrue()
    {
        Assert.True(TypeCompatibility.IsAssignable(TmType.Phrase, TmType.Silence));
    }

    [Theory]
    [InlineData(TypeKind.Sample)]
    [InlineData(TypeKind.Wave)]
    [InlineData(TypeKind.Tone)]
    [InlineData(TypeKind.Signal)]
    public void IsAssignable_SilenceToNonPhrase_IsFalse(TypeKind kind)
    {
        TmType target = kind switch
        {
            TypeKind.Sample => TmType.Sample,
            TypeKind.Wave => TmType.Wave,
            TypeKind.Tone => TmType.Tone,
            _ => TmType.Signal
        };

        Assert.False(TypeCompatibility.IsAssignable(target, TmType.Silence));
        Assert.True(TypeCompatibility.IsSilenceMisuse(target, TmType.Silence));
    }

    [Fact]
    public void IsAssignable_UnknownOnEitherSide_IsTrue()
    {
        // El tipo error no debe generar diagnósticos derivados.
        Assert.True(TypeCompatibility.IsAssignable(TmType.Unknown, TmType.Wave));
        Assert.True(TypeCompatibility.IsAssignable(TmType.Sample, TmType.Unknown));
    }

    [Fact]
    public void IsSilenceMisuse_SilenceToPhrase_IsFalse()
    {
        Assert.False(TypeCompatibility.IsSilenceMisuse(TmType.Phrase, TmType.Silence));
    }

    [Fact]
    public void IsSilenceMisuse_NonSilenceSource_IsFalse()
    {
        Assert.False(TypeCompatibility.IsSilenceMisuse(TmType.Sample, TmType.Wave));
    }
}
