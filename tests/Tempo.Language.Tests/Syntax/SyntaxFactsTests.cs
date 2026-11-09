using Tempo.Language.Syntax;
using Xunit;

namespace Tempo.Language.Tests.Syntax;

/// <summary>
/// La tabla única de palabras reservadas: el Lexer debe reconocer cada fila tal
/// como la describe, y los papeles deben coincidir con la especificación.
/// </summary>

public class SyntaxFactsTests
{
    [Fact]
    public void ReservedWords_ContainsThirtyDistinctWords()
    {
        Assert.Equal(30, SyntaxFacts.ReservedWords.Count);
        Assert.Equal(30, SyntaxFacts.ReservedWords.Select(word => word.Lexeme).Distinct().Count());
    }

    [Fact]
    public void Lexer_TokenizesEveryReservedWordAsItsTableToken()
    {
        foreach (ReservedWord word in SyntaxFacts.ReservedWords)
        {
            Token token = new Lexer(word.Lexeme, "test.tm").Tokenize().First();

            Assert.Equal(word.Token, token.TokenType);
        }
    }

    [Theory]
    [InlineData("sample", ReservedWordRole.PrimitiveType)]
    [InlineData("void", ReservedWordRole.PrimitiveType)]
    [InlineData("sequence", ReservedWordRole.CompositeType)]
    [InlineData("score", ReservedWordRole.CompositeType)]
    [InlineData("silence", ReservedWordRole.Literal)]
    [InlineData("loop", ReservedWordRole.Keyword)]
    public void TryGetReservedWord_ReturnsItsRole(string lexeme, ReservedWordRole expected)
    {
        Assert.True(SyntaxFacts.TryGetReservedWord(lexeme, out ReservedWord word));
        Assert.Equal(expected, word.Role);
    }

    [Theory]
    [InlineData("Sample")]
    [InlineData("edad")]
    [InlineData("")]
    [InlineData(null)]
    public void TryGetReservedWord_RejectsNonReservedLexemes(string lexeme)
    {
        Assert.False(SyntaxFacts.TryGetReservedWord(lexeme, out _));
    }

    [Theory]
    [InlineData('a', true)]
    [InlineData('Z', true)]
    [InlineData('_', false)]
    [InlineData('1', false)]
    [InlineData('ñ', false)]
    public void IsIdentifierStart_AcceptsOnlyAsciiLetters(char value, bool expected)
    {
        Assert.Equal(expected, SyntaxFacts.IsIdentifierStart(value));
    }
}
