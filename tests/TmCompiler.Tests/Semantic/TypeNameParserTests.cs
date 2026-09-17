using TmCompiler.Domain.Semantic;
using Xunit;

namespace TmCompiler.Tests.Semantic;

public sealed class TypeNameParserTests
{
    [Theory]
    [InlineData("sample", TypeKind.Sample)]
    [InlineData("wave", TypeKind.Wave)]
    [InlineData("tone", TypeKind.Tone)]
    [InlineData("phrase", TypeKind.Phrase)]
    [InlineData("signal", TypeKind.Signal)]
    [InlineData("silence", TypeKind.Silence)]
    [InlineData("void", TypeKind.Void)]
    public void TryParse_PrimitiveTypeName_ReturnsMatchingKind(string typeName, TypeKind expected)
    {
        Assert.True(TypeNameParser.TryParse(typeName, out TmType type));
        Assert.Equal(expected, type.Kind);
    }

    [Fact]
    public void TryParse_SequenceTypeName_ReturnsSequenceWithElementType()
    {
        Assert.True(TypeNameParser.TryParse("sequence<sample>", out TmType type));
        Assert.Equal(TmType.SequenceOf(TmType.Sample), type);
    }

    [Fact]
    public void TryParse_ScoreTypeName_ReturnsScoreWithElementType()
    {
        Assert.True(TypeNameParser.TryParse("score<wave>", out TmType type));
        Assert.Equal(TmType.ScoreOf(TmType.Wave), type);
    }

    [Fact]
    public void TryParse_NestedCompositeTypeName_ReturnsNestedType()
    {
        Assert.True(TypeNameParser.TryParse("sequence<score<tone>>", out TmType type));
        Assert.Equal(TmType.SequenceOf(TmType.ScoreOf(TmType.Tone)), type);
    }

    [Fact]
    public void TryParse_CompositeTypeName_RoundTripsThroughToString()
    {
        Assert.True(TypeNameParser.TryParse("score<wave>", out TmType type));
        Assert.Equal("score<wave>", type.ToString());
    }

    [Theory]
    [InlineData("Sample")]
    [InlineData("entero")]
    [InlineData("sequence<>")]
    [InlineData("sequence<nada>")]
    [InlineData("lista<sample>")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParse_UnknownTypeName_ReturnsFalse(string typeName)
    {
        Assert.False(TypeNameParser.TryParse(typeName, out TmType type));
        Assert.True(type.IsUnknown);
    }

    [Fact]
    public void ParseOrUnknown_UnknownTypeName_ReturnsUnknown()
    {
        Assert.True(TypeNameParser.ParseOrUnknown("inexistente").IsUnknown);
    }
}
