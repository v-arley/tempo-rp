namespace Tempo.Language.Syntax;

/// <summary>
/// Papel que cumple una palabra reservada dentro del lenguaje. Lo consultan las
/// herramientas (coloreado, autocompletado), no el compilador.
/// </summary>

public enum ReservedWordRole
{
    /// <summary>Estructura y control: <c>compose</c>, <c>loop</c>, <c>and</c>…</summary>
    Keyword,

    /// <summary>Tipo primitivo o <c>void</c>: <c>sample</c>, <c>phrase</c>…</summary>
    PrimitiveType,

    /// <summary>Tipo compuesto con argumento: <c>sequence&lt;T&gt;</c>, <c>score&lt;T&gt;</c>.</summary>
    CompositeType,

    /// <summary>Valor predefinido: <c>true</c>, <c>false</c>, <c>silence</c>.</summary>
    Literal
}

/// <summary>Una palabra reservada: su lexema, el token que produce y su papel.</summary>
public sealed record ReservedWord(string Lexeme, TokenType Token, ReservedWordRole Role)
{
    public bool IsType => Role is ReservedWordRole.PrimitiveType or ReservedWordRole.CompositeType;
}

/// <summary>
/// Hechos léxicos de TM (§2 de la especificación) en un único lugar: la tabla
/// de palabras reservadas y las reglas de los identificadores. El
/// <see cref="Lexer"/>, el coloreado y el autocompletado del IDE leen de aquí,
/// así que añadir una palabra al lenguaje es añadir una fila a esta tabla.
/// </summary>

public static class SyntaxFacts
{
    private static readonly ReservedWord[] Table =
    [
        new("compose", TokenType.Compose, ReservedWordRole.Keyword),
        new("session", TokenType.Session, ReservedWordRole.Keyword),
        new("output", TokenType.Output, ReservedWordRole.Keyword),
        new("input", TokenType.Input, ReservedWordRole.Keyword),
        new("mix", TokenType.Mix, ReservedWordRole.Keyword),
        new("case", TokenType.Case, ReservedWordRole.Keyword),
        new("default", TokenType.Default, ReservedWordRole.Keyword),
        new("loop", TokenType.Loop, ReservedWordRole.Keyword),
        new("repeat", TokenType.Repeat, ReservedWordRole.Keyword),
        new("if", TokenType.If, ReservedWordRole.Keyword),
        new("else", TokenType.Else, ReservedWordRole.Keyword),
        new("ret", TokenType.Ret, ReservedWordRole.Keyword),
        new("import", TokenType.Import, ReservedWordRole.Keyword),
        new("const", TokenType.Const, ReservedWordRole.Keyword),
        new("and", TokenType.And, ReservedWordRole.Keyword),
        new("or", TokenType.Or, ReservedWordRole.Keyword),
        new("not", TokenType.Not, ReservedWordRole.Keyword),
        new("tempo", TokenType.Tempo, ReservedWordRole.Keyword),
        new("pulse", TokenType.Pulse, ReservedWordRole.Keyword),
        new("sample", TokenType.Sample, ReservedWordRole.PrimitiveType),
        new("wave", TokenType.Wave, ReservedWordRole.PrimitiveType),
        new("tone", TokenType.Tone, ReservedWordRole.PrimitiveType),
        new("phrase", TokenType.Phrase, ReservedWordRole.PrimitiveType),
        new("signal", TokenType.Signal, ReservedWordRole.PrimitiveType),
        new("void", TokenType.Void, ReservedWordRole.PrimitiveType),
        new("sequence", TokenType.Sequence, ReservedWordRole.CompositeType),
        new("score", TokenType.Score, ReservedWordRole.CompositeType),
        new("true", TokenType.True, ReservedWordRole.Literal),
        new("false", TokenType.False, ReservedWordRole.Literal),
        new("silence", TokenType.Silence, ReservedWordRole.Literal)
    ];

    private static readonly Dictionary<string, ReservedWord> ByLexeme =
        Table.ToDictionary(word => word.Lexeme, StringComparer.Ordinal);

    /// <summary>Todas las palabras reservadas, en el orden de la especificación.</summary>
    public static IReadOnlyList<ReservedWord> ReservedWords => Table;

    /// <summary>Busca una palabra reservada; distingue mayúsculas de minúsculas.</summary>
    public static bool TryGetReservedWord(string lexeme, out ReservedWord word)
    {
        word = null;
        return lexeme is not null && ByLexeme.TryGetValue(lexeme, out word);
    }

    /// <summary>
    /// Letra ASCII: el único inicio válido de un identificador o palabra reservada.
    /// EBNF: <c>letter = "a".."z" | "A".."Z";</c>
    /// </summary>
    public static bool IsIdentifierStart(char value) => char.IsAsciiLetter(value);

    /// <summary>EBNF: <c>identifier = letter, { letter | digit | "_" };</c></summary>
    public static bool IsIdentifierPart(char value) => char.IsAsciiLetter(value) || char.IsDigit(value) || value == '_';
}
