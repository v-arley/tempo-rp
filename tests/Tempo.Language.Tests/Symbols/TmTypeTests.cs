using Tempo.Language.Symbols;
using Xunit;

namespace Tempo.Language.Tests.Symbols;

public sealed class TmTypeTests
{
    [Fact]
    public void Equals_TwoSequencesOfTheSameElement_AreEqual()
    {
        Assert.Equal(TmType.SequenceOf(TmType.Sample), TmType.SequenceOf(TmType.Sample));
    }

    [Fact]
    public void Equals_SequencesOfDifferentElements_AreNotEqual()
    {
        Assert.NotEqual(TmType.SequenceOf(TmType.Sample), TmType.SequenceOf(TmType.Wave));
    }

    [Fact]
    public void Equals_SequenceAndScoreOfTheSameElement_AreNotEqual()
    {
        Assert.NotEqual(TmType.SequenceOf(TmType.Sample), TmType.ScoreOf(TmType.Sample));
    }

    [Theory]
    [InlineData(TypeKind.Sample, true)]
    [InlineData(TypeKind.Wave, true)]
    [InlineData(TypeKind.Tone, true)]
    [InlineData(TypeKind.Phrase, true)]
    [InlineData(TypeKind.Signal, true)]
    [InlineData(TypeKind.Silence, false)]
    [InlineData(TypeKind.Void, false)]
    [InlineData(TypeKind.Unknown, false)]
    public void IsPrimitive_ForEachKind_MatchesSpecification(TypeKind kind, bool expected)
    {
        TmType type = kind switch
        {
            TypeKind.Sample => TmType.Sample,
            TypeKind.Wave => TmType.Wave,
            TypeKind.Tone => TmType.Tone,
            TypeKind.Phrase => TmType.Phrase,
            TypeKind.Signal => TmType.Signal,
            TypeKind.Silence => TmType.Silence,
            TypeKind.Void => TmType.Void,
            _ => TmType.Unknown
        };

        Assert.Equal(expected, type.IsPrimitive);
    }

    [Fact]
    public void IsComposite_ForSequenceAndScore_IsTrue()
    {
        Assert.True(TmType.SequenceOf(TmType.Tone).IsComposite);
        Assert.True(TmType.ScoreOf(TmType.Tone).IsComposite);
        Assert.False(TmType.Phrase.IsComposite);
    }

    [Fact]
    public void ToString_ForPrimitiveType_ReturnsTmSpelling()
    {
        Assert.Equal("sample", TmType.Sample.ToString());
        Assert.Equal("phrase", TmType.Phrase.ToString());
    }

    [Fact]
    public void ToString_ForNestedComposite_ReturnsTmSpelling()
    {
        Assert.Equal("score<wave>", TmType.ScoreOf(TmType.Wave).ToString());
        Assert.Equal(
            "sequence<score<sample>>",
            TmType.SequenceOf(TmType.ScoreOf(TmType.Sample)).ToString());
    }
}
