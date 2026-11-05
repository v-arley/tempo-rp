using System.Text;
using Tempo.Language.Diagnostics;

namespace Tempo.Language.Syntax;

/// <summary>
/// Analizador léxico del lenguaje TM: convierte el texto fuente en una
/// secuencia de <see cref="Token"/>.
///
/// Recorre la fuente carácter por carácter, descarta los comentarios y el
/// espacio en blanco no significativo, agrupa el resto en tokens según las
/// reglas léxicas de TM v1.1 y cierra la secuencia con un token
/// <see cref="TokenType.EndOfFile"/>. Un carácter que no encaja en ninguna
/// regla produce una <see cref="LexerException"/> con la línea y la columna
/// donde aparece.
///
/// La clase es <c>sealed</c> y mantiene su cursor de lectura (posición, línea
/// y columna) en campos privados; el único punto de interacción es
/// <see cref="Tokenize"/>.
/// </summary>

public sealed class Lexer : ILexer
{
    // Texto fuente y el nombre de archivo que acompaña a los errores.
    private readonly string _source;
    private readonly string _fileName;
    private readonly int _length;

    private int _position;   // Índice actual en _source (0-based)
    private int _line;       // Línea actual (1-based)
    private int _column;     // Columna actual (1-based)

    // Palabras reservadas de TM indexadas por su lexema. Permite decidir en
    // tiempo constante si una secuencia de letras es una keyword o un
    // identificador de usuario.
    private static readonly Dictionary<string, TokenType> ReservedWords;

    // Puebla la tabla de palabras reservadas una sola vez para toda la clase.
    static Lexer()
    {
        ReservedWords = new Dictionary<string, TokenType>(StringComparer.Ordinal)
            {
                { "compose", TokenType.Compose },
                { "session", TokenType.Session },
                { "output", TokenType.Output },
                { "input", TokenType.Input },
                { "mix", TokenType.Mix },
                { "case", TokenType.Case },
                { "default", TokenType.Default },
                { "loop", TokenType.Loop },
                { "repeat", TokenType.Repeat },
                { "if", TokenType.If },
                { "else", TokenType.Else },
                { "ret", TokenType.Ret },
                { "import", TokenType.Import },
                { "const", TokenType.Const },
                { "sequence", TokenType.Sequence },
                { "score", TokenType.Score },
                { "true", TokenType.True },
                { "false", TokenType.False },
                { "silence", TokenType.Silence },
                { "sample", TokenType.Sample },
                { "wave", TokenType.Wave },
                { "tone", TokenType.Tone },
                { "phrase", TokenType.Phrase },
                { "signal", TokenType.Signal },
                { "void", TokenType.Void },
                { "and", TokenType.And },
                { "or", TokenType.Or },
                { "not", TokenType.Not },
                { "tempo", TokenType.Tempo },
                { "pulse", TokenType.Pulse }
            };
    }

    /// <summary>
    /// Crea un analizador léxico posicionado al inicio de <paramref name="source"/>.
    /// </summary>
    /// <param name="source">Contenido completo del archivo fuente TM.</param>
    /// <param name="fileName">
    /// Nombre del archivo que aparecerá en los mensajes de error; si es null se
    /// usa <c>&lt;unknown&gt;</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">Si <paramref name="source"/> es null.</exception>
    public Lexer(string source, string fileName)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _fileName = fileName ?? "<unknown>";
        _length = _source.Length;
        _position = 0;
        _line = 1;
        _column = 1;
    }

    // Punto de entrada público (contrato ILexer).

    /// <summary>
    /// Recorre toda la fuente y devuelve sus tokens en orden de aparición.
    /// Alterna entre saltar espacio y comentarios y leer un token, delegando
    /// el reconocimiento de cada uno en el método especializado que le
    /// corresponde.
    /// </summary>
    /// <returns>
    /// La secuencia de tokens, cerrada siempre con un token
    /// <see cref="TokenType.EndOfFile"/>.
    /// </returns>
    public IEnumerable<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (!IsAtEnd())
        {
            SkipWhitespaceAndComments();

            if (IsAtEnd())
            {
                break;
            }

            // Un error léxico se señala lanzando LexerException, nunca con un
            // token de tipo Error: o se obtiene la secuencia completa, o la
            // excepción se propaga al llamador.
            Token token = ReadNextToken();
            tokens.Add(token);
        }

        tokens.Add(CreateToken(TokenType.EndOfFile, string.Empty));
        return tokens;
    }

    // Lectura de la fuente

    /// <summary>
    /// Indica si el cursor ya pasó el último carácter de la fuente.
    /// </summary>
    private bool IsAtEnd() => _position >= _length;

    /// <summary>
    /// Retorna el carácter actual sin avanzar la posición.
    /// </summary>
    private char Peek() => IsAtEnd() ? '\0' : _source[_position];

    /// <summary>
    /// Retorna el carácter que sigue al actual sin avanzar. Da el segundo
    /// carácter que necesitan los operadores compuestos (>=, <=, ==, !=, >>,
    /// <<, ++, --) y la decisión de si un punto abre la parte decimal.
    /// </summary>
    private char PeekNext() => (_position + 1 >= _length) ? '\0' : _source[_position + 1];

    /// <summary>
    /// Consume y retorna el carácter actual, avanzando la posición,
    /// línea y columna según corresponda.
    /// </summary>
    private char Advance()
    {
        if (IsAtEnd())
        {
            return '\0';
        }

        char current = _source[_position];
        _position++;

        if (current == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }

        return current;
    }

    /// <summary>
    /// Crea un token con los metadatos de ubicación actuales (antes de avanzar).
    /// </summary>
    private Token CreateToken(TokenType type, string lexeme, int lineOffset = 0, int colOffset = 0) =>
        new Token(type, lexeme, _line + lineOffset, _column + colOffset);

    /// <summary>
    /// Avanza el cursor sobre todo el espacio en blanco y los comentarios (de
    /// línea <c>#</c> y de bloque <c>## ##</c>) hasta el siguiente carácter
    /// significativo o el fin de la fuente. Ni el espacio en blanco ni los
    /// comentarios producen tokens.
    /// </summary>
    private void SkipWhitespaceAndComments()
    {
        while (!IsAtEnd())
        {
            char c = Peek();

            // Whitespace: espacio, tabulación, retorno de carro, nueva línea
            if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
            {
                Advance();
                continue;
            }

            // Comentario de línea: # ... hasta fin de línea
            if (c == '#' && PeekNext() != '#')
            {
                SkipLineComment();
                continue;
            }

            // Comentario de bloque: ## ... ##
            if (c == '#' && PeekNext() == '#')
            {
                SkipBlockComment();
                continue;
            }

            break;
        }
    }

    /// <summary>
    /// Consume desde el <c>#</c> actual hasta el final de la línea física, sin
    /// incluir el salto de línea.
    /// </summary>
    private void SkipLineComment()
    {
        Advance(); // el '#'
        while (!IsAtEnd() && Peek() != '\n')
        {
            Advance();
        }
        // El '\n' queda sin consumir para que Advance() lo cuente como cambio
        // de línea en la siguiente pasada.
    }

    /// <summary>
    /// Consume un comentario de bloque, desde el <c>##</c> de apertura hasta el
    /// primer <c>##</c> de cierre. Los comentarios de bloque no anidan:
    /// un <c>##</c> interior cierra el comentario.
    /// </summary>
    /// <exception cref="LexerException">
    /// Si la fuente termina sin que aparezca el <c>##</c> de cierre.
    /// </exception>
    private void SkipBlockComment()
    {
        Advance(); // primer '#' de apertura
        Advance(); // segundo '#' de apertura

        while (!IsAtEnd())
        {
            if (Peek() == '#' && PeekNext() == '#')
            {
                Advance(); // primer '#' de cierre
                Advance(); // segundo '#' de cierre
                return;
            }
            Advance();
        }

        throw new LexerException(
            DiagnosticCodes.UnterminatedBlockComment,
            "Comentario de bloque no cerrado. Se esperaba '##' antes del fin de archivo.",
            _fileName, _line, _column);
    }

    /// <summary>
    /// Lee un token completo a partir del carácter actual y elige el
    /// reconocedor según ese carácter: una letra abre un identificador o
    /// palabra reservada, un dígito un literal numérico, la comilla doble un
    /// literal de cadena, la comilla simple un literal de carácter y cualquier
    /// otro carácter un operador o signo de puntuación.
    /// </summary>
    private Token ReadNextToken()
    {
        char c = Peek();
        int startLine = _line;
        int startColumn = _column;

        // Identificadores y palabras reservadas
        if (IsLetter(c))
        {
            return ReadIdentifierOrKeyword(startLine, startColumn);
        }

        // Literales numéricos (enteros y flotantes)
        if (char.IsDigit(c))
        {
            return ReadNumericLiteral(startLine, startColumn);
        }

        // Literales de cadena (phrase)
        if (c == '"')
        {
            return ReadStringLiteral(startLine, startColumn);
        }

        // Literales de carácter (tone)
        if (c == '\'')
        {
            return ReadCharLiteral(startLine, startColumn);
        }

        // Operadores y símbolos de puntuación de 1 o 2 caracteres
        return ReadOperatorOrPunctuation(c, startLine, startColumn);
    }

    // Reconocedores por categoría de token

    /// <summary>
    /// Lee una palabra: una letra inicial seguida de letras, dígitos o guion
    /// bajo. Si el lexema figura en la tabla de palabras reservadas devuelve la
    /// keyword correspondiente; si no, un identificador. La comparación
    /// distingue mayúsculas de minúsculas.
    /// EBNF: identifier = letter, { letter | digit | "_" };
    /// </summary>
    private Token ReadIdentifierOrKeyword(int startLine, int startColumn)
    {
        var lexemeBuilder = new StringBuilder();

        while (!IsAtEnd() && IsIdentifierPart(Peek()))
        {
            lexemeBuilder.Append(Advance());
        }

        string lexeme = lexemeBuilder.ToString();

        if (ReservedWords.TryGetValue(lexeme, out TokenType keywordType))
        {
            return new Token(keywordType, lexeme, startLine, startColumn);
        }

        return new Token(TokenType.Identifier, lexeme, startLine, startColumn);
    }

    /// <summary>
    /// Lee un literal numérico: entero (sample) o, si hay un punto seguido de
    /// al menos un dígito, flotante (wave). El lexema empieza siempre por un
    /// dígito y nunca lleva signo; un <c>-</c> delante es un token
    /// <see cref="TokenType.Minus"/> aparte.
    /// EBNF:
    ///   integer_literal = digit, { digit };
    ///   float_literal   = digit, { digit }, ".", digit, { digit };
    /// </summary>
    private Token ReadNumericLiteral(int startLine, int startColumn)
    {
        var lexemeBuilder = new StringBuilder();
        bool isFloat = false;

        // Parte entera: al menos un dígito.
        if (!char.IsDigit(Peek()))
        {
            throw new LexerException(
                DiagnosticCodes.MalformedNumericLiteral,
                "Se esperaba al menos un dígito en el literal numérico.",
                _fileName, _line, _column);
        }

        while (!IsAtEnd() && char.IsDigit(Peek()))
        {
            lexemeBuilder.Append(Advance());
        }

        // El punto solo pertenece al número si va seguido de un dígito.
        if (Peek() == '.' && char.IsDigit(PeekNext()))
        {
            isFloat = true;
            lexemeBuilder.Append(Advance());

            while (!IsAtEnd() && char.IsDigit(Peek()))
            {
                lexemeBuilder.Append(Advance());
            }
        }
        else if (Peek() == '.' && !IsIdentifierPart(PeekNext()))
        {
            throw new LexerException(
                DiagnosticCodes.MalformedNumericLiteral,
                "Se esperaba un dígito después del punto decimal.",
                _fileName, _line, _column);
        }

        string lexeme = lexemeBuilder.ToString();
        TokenType type = isFloat ? TokenType.FloatLiteral : TokenType.IntegerLiteral;
        return new Token(type, lexeme, startLine, startColumn);
    }

    /// <summary>
    /// Reconoce literales de cadena (phrase): delimitados por comillas dobles.
    /// Soporta secuencias de escape: \n, \t, \", \\
    /// EBNF: string_literal = "\"", { character | escape_sequence }, "\"";
    /// </summary>
    /// <exception cref="LexerException">Si la cadena no se cierra antes de EOF o nueva línea.</exception>
    private Token ReadStringLiteral(int startLine, int startColumn)
    {
        var lexemeBuilder = new StringBuilder();
        Advance(); // consumir comilla de apertura "

        while (!IsAtEnd() && Peek() != '"')
        {
            char c = Peek();

            if (c == '\n')
            {
                throw new LexerException(
                    DiagnosticCodes.UnterminatedString,
                    "Literal de cadena no cerrado. Las cadenas no pueden abarcar múltiples líneas.",
                    _fileName, _line, _column);
            }

            if (c == '\\')
            {
                Advance(); // consume '\'
                if (IsAtEnd())
                {
                    throw new LexerException(
                        DiagnosticCodes.InvalidEscapeSequence,
                        "Secuencia de escape incompleta al final del archivo.",
                        _fileName, _line, _column);
                }

                char escapeChar = Advance();
                char resolved = escapeChar switch
                {
                    'n' => '\n',
                    't' => '\t',
                    '"' => '"',
                    '\\' => '\\',
                    _ => throw new LexerException(
                        DiagnosticCodes.InvalidEscapeSequence,
                        $"Secuencia de escape no válida: '\\{escapeChar}'. Use \\n, \\t, \\\" o \\\\\\",
                        _fileName, _line, _column)
                };
                lexemeBuilder.Append(resolved);
            }
            else
            {
                lexemeBuilder.Append(Advance());
            }
        }

        if (IsAtEnd())
        {
            throw new LexerException(
                DiagnosticCodes.UnterminatedString,
                "Literal de cadena no cerrado. Se esperaba '\"' antes del fin de archivo.",
                _fileName, _line, _column);
        }

        Advance(); // consumir comilla de cierre "
        return new Token(TokenType.StringLiteral, lexemeBuilder.ToString(), startLine, startColumn);
    }

    /// <summary>
    /// Reconoce literales de carácter (tone): delimitados por comillas simples,
    /// conteniendo exactamente un carácter o una secuencia de escape válida.
    /// EBNF: char_literal = "'", ( character | escape_sequence ), "'";
    /// </summary>
    private Token ReadCharLiteral(int startLine, int startColumn)
    {
        Advance(); // consumir comilla simple de apertura '

        if (IsAtEnd())
        {
            throw new LexerException(
                DiagnosticCodes.MalformedCharacterLiteral,
                "Literal de carácter incompleto al final del archivo.",
                _fileName, _line, _column);
        }

        char value;
        char c = Peek();

        if (c == '\n')
        {
            throw new LexerException(
                DiagnosticCodes.MalformedCharacterLiteral,
                "Literal de carácter no puede contener nueva línea.",
                _fileName, _line, _column);
        }

        if (c == '\\')
        {
            Advance(); // consume '\'
            if (IsAtEnd())
            {
                throw new LexerException(
                    DiagnosticCodes.InvalidEscapeSequence,
                    "Secuencia de escape incompleta en literal de carácter.",
                    _fileName, _line, _column);
            }

            char escapeChar = Advance();
            value = escapeChar switch
            {
                'n' => '\n',
                't' => '\t',
                '\'' => '\'',
                '\\' => '\\',
                _ => throw new LexerException(
                    DiagnosticCodes.InvalidEscapeSequence,
                    $"Secuencia de escape no válida en carácter: '\\{escapeChar}'.",
                    _fileName, _line, _column)
            };
        }
        else
        {
            value = Advance();
        }

        if (IsAtEnd() || Peek() != '\'')
        {
            throw new LexerException(
                DiagnosticCodes.MalformedCharacterLiteral,
                "Literal de carácter no cerrado o contiene múltiples caracteres. Se esperaba \"'\".",
                _fileName, _line, _column);
        }

        Advance(); // consumir comilla simple de cierre '
        return new Token(TokenType.CharLiteral, value.ToString(), startLine, startColumn);
    }

    /// <summary>
    /// Lee un operador, un delimitador o un signo de puntuación. Mira el
    /// carácter siguiente para formar los operadores de dos caracteres (==, !=,
    /// >=, <=, >>, <<, ++, --) antes de considerar los de uno solo.
    /// </summary>
    private Token ReadOperatorOrPunctuation(char c, int startLine, int startColumn)
    {
        char next = PeekNext();

        switch (c)
        {
            // Operadores de dos caracteres: se prueban primero.
            case '>' when next == '>':
                Advance(); Advance();
                return new Token(TokenType.OutputOp, ">>", startLine, startColumn);

            case '<' when next == '<':
                Advance(); Advance();
                return new Token(TokenType.InputOp, "<<", startLine, startColumn);

            case '=' when next == '=':
                Advance(); Advance();
                return new Token(TokenType.Equal, "==", startLine, startColumn);

            case '!' when next == '=':
                Advance(); Advance();
                return new Token(TokenType.NotEqual, "!=", startLine, startColumn);

            case '>' when next == '=':
                Advance(); Advance();
                return new Token(TokenType.GreaterOrEqual, ">=", startLine, startColumn);

            case '<' when next == '=':
                Advance(); Advance();
                return new Token(TokenType.LessOrEqual, "<=", startLine, startColumn);

            case '+' when next == '+':
                Advance(); Advance();
                return new Token(TokenType.Increment, "++", startLine, startColumn);

            case '-' when next == '-':
                Advance(); Advance();
                return new Token(TokenType.Decrement, "--", startLine, startColumn);

            // '>' y '<' a solas se emiten siempre como tokens relacionales
            // (GreaterThan / LessThan). Su posible papel como delimitadores de
            // bloque depende de la posición en la gramática (lo decide el Parser) y queda
            // fuera del análisis léxico.
            case '>':
                Advance();
                return new Token(TokenType.GreaterThan, ">", startLine, startColumn);

            case '<':
                Advance();
                return new Token(TokenType.LessThan, "<", startLine, startColumn);

            // Operadores y símbolos de un carácter
            case '+': 
                Advance(); 
                return new Token(TokenType.Plus, "+", startLine, startColumn);
                
            case '-': 
                Advance(); 
                return new Token(TokenType.Minus, "-", startLine, startColumn);
                
            case '*': 
                Advance(); 
                return new Token(TokenType.Multiply, "*", startLine, startColumn);
                
            case '/': 
                Advance(); 
                return new Token(TokenType.Divide, "/", startLine, startColumn);
                
            case '=': 
                Advance(); 
                return new Token(TokenType.Assign, "=", startLine, startColumn);
                
            case '(': 
                Advance(); 
                return new Token(TokenType.LeftParen, "(", startLine, startColumn);
                
            case ')': 
                Advance(); 
                return new Token(TokenType.RightParen, ")", startLine, startColumn);
                
            case '[': 
                Advance(); 
                return new Token(TokenType.LeftBracket, "[", startLine, startColumn);
                
            case ']': 
                Advance(); 
                return new Token(TokenType.RightBracket, "]", startLine, startColumn);
                
            case '{': 
                Advance(); 
                return new Token(TokenType.LeftBrace, "{", startLine, startColumn);
                
            case '}': 
                Advance(); 
                return new Token(TokenType.RightBrace, "}", startLine, startColumn);
                
            case ':': 
                Advance(); 
                return new Token(TokenType.Colon, ":", startLine, startColumn);
                
            case ';': 
                Advance(); 
                return new Token(TokenType.Semicolon, ";", startLine, startColumn);
                
            case ',': 
                Advance(); 
                return new Token(TokenType.Comma, ",", startLine, startColumn);
                
            case '.': 
                Advance(); 
                return new Token(TokenType.Dot, ".", startLine, startColumn);
                

            default:
                Advance();
                throw new LexerException(
                    DiagnosticCodes.InvalidCharacter,
                    $"Carácter inesperado '{c}' (U+{(int)c:X4}). No forma parte del alfabeto léxico de TM.",
                    _fileName, startLine, startColumn);
        }
    }

    // Clasificación de caracteres

    /// <summary>
    /// Indica si el carácter es una letra ASCII, el único inicio válido de un
    /// identificador o palabra reservada en TM.
    /// EBNF: letter = "a".."z" | "A".."Z";
    /// </summary>
    private static bool IsLetter(char c) =>
        (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

    /// <summary>
    /// Indica si el carácter puede aparecer después del primero en un
    /// identificador: una letra, un dígito o un guion bajo.
    /// </summary>
    private static bool IsIdentifierPart(char c) =>
        IsLetter(c) || char.IsDigit(c) || c == '_';
}