namespace Tempo.Language.Syntax;

/// <summary>
/// Enumeración exhaustiva de todos los tipos de tokens reconocidos
/// por la gramática léxica de TM v1.1, organizados por categoría
/// para facilitar el mantenimiento y la extensión futura del lenguaje.
/// </summary>

public enum TokenType
{
    // ---- Palabras reservadas (keywords) ----
    Compose, Session, Output, Input, Mix, Case, Default, Loop, Repeat,
    If, Else, Ret, Import, Const, Sequence, Score,
    True, False, Silence, Sample, Wave, Tone, Phrase, Signal, Void,
    
    // ---- Literales ----
    IntegerLiteral,   // sample
    FloatLiteral,     // wave
    StringLiteral,    // phrase
    CharLiteral,      // tone
    BoolLiteral,      // true | false
    NullLiteral,      // silence
    
    // ---- Identificadores ----
    Identifier,
    
    // ---- Operadores aritméticos ----
    Plus, Minus, Multiply, Divide,
    Increment, Decrement,
    
    // ---- Operadores relacionales ----
    Equal, NotEqual, GreaterThan, LessThan, GreaterOrEqual, LessOrEqual,
    
    // ---- Operadores lógicos ----
    And, Or, Not,
    
    // ---- Operadores de asignación y E/S ----
    Assign, OutputOp, InputOp,
    
    // ---- Símbolos de puntuación / delimitadores ----
    LeftParen, RightParen,      // ( )
    LeftBracket, RightBracket,  // [ ]
    LeftBrace, RightBrace,      // { }
    Colon, Semicolon, Comma, Dot,

    // ---- Fin de archivo ----
    EndOfFile,
    
    // ---- Error léxico ----
    Error
}