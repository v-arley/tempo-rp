using Tempo.Language.Syntax;
using Xunit;

namespace Tempo.Language.Tests.Syntax;

public class LexerTests
{
    private static List<Token> Tokenize(string source) =>
        new Lexer(source, "test.tm").Tokenize().ToList();

    // Fuente vacía / EOF

    [Fact]
    public void Tokenize_EmptySource_ReturnsOnlyEndOfFile()
    {
        var tokens = Tokenize("");

        Assert.Single(tokens);
        Assert.Equal(TokenType.EndOfFile, tokens[0].TokenType);
        Assert.Equal(string.Empty, tokens[0].Lexeme);
    }

    [Fact]
    public void Tokenize_AnySource_AlwaysEndsWithEndOfFile()
    {
        var tokens = Tokenize("x = 1;");

        Assert.Equal(TokenType.EndOfFile, tokens[^1].TokenType);
    }

    // Palabras reservadas

    [Theory]
    [InlineData("compose", TokenType.Compose)]
    [InlineData("session", TokenType.Session)]
    [InlineData("output", TokenType.Output)]
    [InlineData("input", TokenType.Input)]
    [InlineData("mix", TokenType.Mix)]
    [InlineData("case", TokenType.Case)]
    [InlineData("default", TokenType.Default)]
    [InlineData("loop", TokenType.Loop)]
    [InlineData("repeat", TokenType.Repeat)]
    [InlineData("if", TokenType.If)]
    [InlineData("else", TokenType.Else)]
    [InlineData("ret", TokenType.Ret)]
    [InlineData("import", TokenType.Import)]
    [InlineData("const", TokenType.Const)]
    [InlineData("sequence", TokenType.Sequence)]
    [InlineData("score", TokenType.Score)]
    [InlineData("true", TokenType.True)]
    [InlineData("false", TokenType.False)]
    [InlineData("silence", TokenType.Silence)]
    [InlineData("sample", TokenType.Sample)]
    [InlineData("wave", TokenType.Wave)]
    [InlineData("tone", TokenType.Tone)]
    [InlineData("phrase", TokenType.Phrase)]
    [InlineData("signal", TokenType.Signal)]
    [InlineData("void", TokenType.Void)]
    [InlineData("and", TokenType.And)]
    [InlineData("or", TokenType.Or)]
    [InlineData("not", TokenType.Not)]
    public void Tokenize_ReservedWord_ProducesExpectedKeywordToken(string keyword, TokenType expected)
    {
        var tokens = Tokenize(keyword);

        Assert.Equal(expected, tokens[0].TokenType);
        Assert.Equal(keyword, tokens[0].Lexeme);
    }

    // Identificadores

    [Theory]
    [InlineData("miVariable")]
    [InlineData("valor2")]
    [InlineData("A_B_c123")]
    public void Tokenize_ValidIdentifier_ProducesIdentifierToken(string identifier)
    {
        var tokens = Tokenize(identifier);

        Assert.Equal(TokenType.Identifier, tokens[0].TokenType);
        Assert.Equal(identifier, tokens[0].Lexeme);
    }

    [Fact]
    public void Tokenize_Identifiers_AreCaseSensitive()
    {
        var tokens = Tokenize("True true");

        Assert.Equal(TokenType.Identifier, tokens[0].TokenType); // "True" no es keyword
        Assert.Equal(TokenType.True, tokens[1].TokenType);       // "true" sí
    }

    // Literales numéricos

    [Fact]
    public void Tokenize_IntegerLiteral_ProducesIntegerToken()
    {
        var tokens = Tokenize("42");

        Assert.Equal(TokenType.IntegerLiteral, tokens[0].TokenType);
        Assert.Equal("42", tokens[0].Lexeme);
    }

    [Fact]
    public void Tokenize_FloatLiteral_ProducesFloatToken()
    {
        var tokens = Tokenize("3.14");

        Assert.Equal(TokenType.FloatLiteral, tokens[0].TokenType);
        Assert.Equal("3.14", tokens[0].Lexeme);
    }

    [Fact]
    public void Tokenize_DotNotFollowedByDigit_IsNotConsumedAsFloat()
    {
        // "3.x" -> IntegerLiteral "3", Dot, Identifier "x"
        var tokens = Tokenize("3.x");

        Assert.Equal(TokenType.IntegerLiteral, tokens[0].TokenType);
        Assert.Equal("3", tokens[0].Lexeme);
        Assert.Equal(TokenType.Dot, tokens[1].TokenType);
        Assert.Equal(TokenType.Identifier, tokens[2].TokenType);
    }

    [Fact]
    public void Tokenize_LeadingMinusBeforeNumber_IsSeparateMinusToken()
    {
        // El dispatcher solo entra a ReadNumericLiteral si el carácter actual es dígito,
        // por lo que "-7" se tokeniza como Minus + IntegerLiteral, NO como un literal negativo.
        var tokens = Tokenize("-7");

        Assert.Equal(TokenType.Minus, tokens[0].TokenType);
        Assert.Equal(TokenType.IntegerLiteral, tokens[1].TokenType);
        Assert.Equal("7", tokens[1].Lexeme);
    }

    // Literales de cadena (phrase)

    [Fact]
    public void Tokenize_StringLiteral_ProducesStringToken()
    {
        var tokens = Tokenize("\"hola mundo\"");

        Assert.Equal(TokenType.StringLiteral, tokens[0].TokenType);
        Assert.Equal("hola mundo", tokens[0].Lexeme);
    }

    [Fact]
    public void Tokenize_StringLiteral_ResolvesEscapeSequences()
    {
        var tokens = Tokenize("\"a\\nb\\tc\\\"d\\\\e\"");

        Assert.Equal("a\nb\tc\"d\\e", tokens[0].Lexeme);
    }

    [Fact]
    public void Tokenize_UnterminatedString_ThrowsLexerException()
    {
        Assert.Throws<LexerException>(() => Tokenize("\"sin cerrar"));
    }

    [Fact]
    public void Tokenize_StringSpanningNewline_ThrowsLexerException()
    {
        Assert.Throws<LexerException>(() => Tokenize("\"linea1\nlinea2\""));
    }

    [Fact]
    public void Tokenize_StringWithInvalidEscape_ThrowsLexerException()
    {
        Assert.Throws<LexerException>(() => Tokenize("\"\\q\""));
    }

    // Literales de carácter (tone)

    [Fact]
    public void Tokenize_CharLiteral_ProducesCharToken()
    {
        var tokens = Tokenize("'a'");

        Assert.Equal(TokenType.CharLiteral, tokens[0].TokenType);
        Assert.Equal("a", tokens[0].Lexeme);
    }

    [Fact]
    public void Tokenize_CharLiteral_ResolvesEscapeSequence()
    {
        var tokens = Tokenize("'\\n'");

        Assert.Equal("\n", tokens[0].Lexeme);
    }

    [Fact]
    public void Tokenize_CharLiteralWithMultipleChars_ThrowsLexerException()
    {
        Assert.Throws<LexerException>(() => Tokenize("'ab'"));
    }

    // Operadores y puntuación

    [Theory]
    [InlineData(">>", TokenType.OutputOp)]
    [InlineData("<<", TokenType.InputOp)]
    [InlineData("==", TokenType.Equal)]
    [InlineData("!=", TokenType.NotEqual)]
    [InlineData(">=", TokenType.GreaterOrEqual)]
    [InlineData("<=", TokenType.LessOrEqual)]
    [InlineData("++", TokenType.Increment)]
    [InlineData("--", TokenType.Decrement)]
    [InlineData(">", TokenType.GreaterThan)]
    [InlineData("<", TokenType.LessThan)]
    [InlineData("+", TokenType.Plus)]
    [InlineData("-", TokenType.Minus)]
    [InlineData("*", TokenType.Multiply)]
    [InlineData("/", TokenType.Divide)]
    [InlineData("=", TokenType.Assign)]
    [InlineData("(", TokenType.LeftParen)]
    [InlineData(")", TokenType.RightParen)]
    [InlineData("[", TokenType.LeftBracket)]
    [InlineData("]", TokenType.RightBracket)]
    [InlineData("{", TokenType.LeftBrace)]
    [InlineData("}", TokenType.RightBrace)]
    [InlineData(":", TokenType.Colon)]
    [InlineData(";", TokenType.Semicolon)]
    [InlineData(",", TokenType.Comma)]
    [InlineData(".", TokenType.Dot)]
    public void Tokenize_Operator_ProducesExpectedToken(string lexeme, TokenType expected)
    {
        var tokens = Tokenize(lexeme);

        Assert.Equal(expected, tokens[0].TokenType);
        Assert.Equal(lexeme, tokens[0].Lexeme);
    }

    [Fact]
    public void Tokenize_UnexpectedCharacter_ThrowsLexerException()
    {
        Assert.Throws<LexerException>(() => Tokenize("@"));
    }

    // Comentarios y espacios en blanco

    [Fact]
    public void Tokenize_LineComment_IsIgnored()
    {
        var tokens = Tokenize("# esto es un comentario\nidentificador");

        Assert.Equal(TokenType.Identifier, tokens[0].TokenType);
        Assert.Equal("identificador", tokens[0].Lexeme);
        Assert.Equal(TokenType.EndOfFile, tokens[1].TokenType);
    }

    [Fact]
    public void Tokenize_BlockComment_IsIgnored()
    {
        var tokens = Tokenize("## esto\nes un\nbloque ## resto");

        Assert.Equal(TokenType.Identifier, tokens[0].TokenType);
        Assert.Equal("resto", tokens[0].Lexeme);
    }

    [Fact]
    public void Tokenize_UnterminatedBlockComment_ThrowsLexerException()
    {
        Assert.Throws<LexerException>(() => Tokenize("## nunca se cierra"));
    }

    [Fact]
    public void Tokenize_Whitespace_IsSkippedBetweenTokens()
    {
        var tokens = Tokenize("  \t\r\n  x");

        Assert.Equal(TokenType.Identifier, tokens[0].TokenType);
        Assert.Equal("x", tokens[0].Lexeme);
    }

    // Seguimiento de línea / columna


    [Fact]
    public void Tokenize_TracksLineAndColumnAcrossMultipleLines()
    {
        var tokens = Tokenize("a\nbb\n  c");

        Assert.Equal(1, tokens[0].Line);
        Assert.Equal(1, tokens[0].Column);

        Assert.Equal(2, tokens[1].Line);
        Assert.Equal(1, tokens[1].Column);

        Assert.Equal(3, tokens[2].Line);
        Assert.Equal(3, tokens[2].Column);
    }

    // Prueba de integración: snippet completo

    [Fact]
    public void Tokenize_FullSnippet_ProducesExpectedTokenSequence()
    {
        const string source = "const x = 10;\nif x >= 5 > ret true; <";

        var tokens = Tokenize(source);
        var types = tokens.Select(t => t.TokenType).ToArray();

        Assert.Equal(new[]
        {
            TokenType.Const, TokenType.Identifier, TokenType.Assign, TokenType.IntegerLiteral, TokenType.Semicolon,
            TokenType.If, TokenType.Identifier, TokenType.GreaterOrEqual, TokenType.IntegerLiteral, TokenType.GreaterThan,
            TokenType.Ret, TokenType.True, TokenType.Semicolon, TokenType.LessThan,
            TokenType.EndOfFile
        }, types);
    }

    [Fact]
    public void Tokenize_RelationalGreaterThanInsideBrackets_IsGreaterThanNotBlockDelimiter()
    {
        // Ejemplo de la especificación §3.2.1: "if [a > b] >" — el '>' dentro de los
        // corchetes es relacional; el '>' tras el corchete de cierre es, en la gramática,
        // el mismo tipo de token (GreaterThan) que el Parser interpretará como inicio de
        // bloque según la posición sintáctica, no algo que el Lexer deba distinguir.
        var tokens = Tokenize("if [a > b] >");
        var types = tokens.Select(t => t.TokenType).ToArray();

        Assert.Equal(new[]
        {
            TokenType.If, TokenType.LeftBracket, TokenType.Identifier, TokenType.GreaterThan,
            TokenType.Identifier, TokenType.RightBracket, TokenType.GreaterThan,
            TokenType.EndOfFile
        }, types);
    }
}
