using System.Text;
using TmCompiler.Domain.Lexical;

namespace TmCompiler.Application.Lexing;

/// <summary>
/// Implementación del analizador léxico para el lenguaje TM.
/// 
/// Responsabilidades principales (SRP):
/// 1. Consumir la fuente carácter por carácter.
/// 2. Descartar comentarios y espacios en blanco no significativos.
/// 3. Reconocer tokens según las reglas léxicas de TM v1.1.
/// 4. Reportar errores léxicos con ubicación precisa.
///
/// Diseño:
/// - Se implementa como clase sellada (<c>sealed</c>) para evitar
///   derivación no controlada que pudiera violar invariantes internas.
/// - El estado del lexer (posición, línea, columna) se encapsula;
///   no es expuesto directamente a consumidores externos.
/// </summary>

public sealed class Lexer : ILexer
{
    // Campos privados (encapsulación del estado interno)
    private readonly string _source;
    private readonly string _fileName;
    private readonly int _length;

    private int _position;   // Índice actual en _source (0-based)
    private int _line;       // Línea actual (1-based)
    private int _column;     // Columna actual (1-based)

    // Mapa estático de palabras reservadas para lookup O(1).
    // Se inicializa una única vez gracias al constructor estático,
    // reduciendo la complejidad de reconocimiento de keywords.
    private static readonly Dictionary<string, TokenType> ReservedWords;

    // Constructor estático: pobla el diccionario de palabras reservadas.
    static Lexer()
    {
        ReservedWords = new Dictionary<string, TokenType>(StringComparer.Ordinal)
            {
                { "compose", TokenType.Compose },
                { "session", TokenType.Session },
                { "output", TokenType.Output },
                { "input", TokenType.Input },
                { "mix", TokenType.Mix },
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
                { "not", TokenType.Not }
            };
    }

    // Constructor de instancia

    /// <summary>
    /// Inicializa una nueva instancia del analizador léxico.
    /// </summary>
    /// <param name="source">Contenido completo del archivo fuente TM.</param>
    /// <param name="fileName">Nombre del archivo para reportes de error.</param>
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

    // MÉTODO PÚBLICO: Tokenize (cumple contrato ILexer)
    
    /// <summary>
    /// Ejecuta el análisis léxico completo. Es el único punto de entrada
    /// público para obtener la lista de tokens, cumpliendo con el principio
    /// de Responsabilidad Única (SRP): este método orquesta el pipeline
    /// de tokenización sin realizar él mismo el reconocimiento de patrones.
    /// </summary>
    /// <returns>Secuencia de tokens, terminada obligatoriamente con EOF.</returns>
    public IEnumerable<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (!IsAtEnd())
        {
            SkipWhitespaceAndComments();

            if (IsAtEnd())
                break;

            // ReadNextToken() nunca retorna TokenType.Error: todo error léxico
            // se reporta lanzando LexerException. Tokenize() por tanto o completa
            // el recorrido, o propaga la excepción — no hay estado intermedio de error.
            Token token = ReadNextToken();
            tokens.Add(token);
        }

        tokens.Add(CreateToken(TokenType.EndOfFile, string.Empty));
        return tokens;
    }

    // SECCIÓN PRIVADA: Máquina de estados léxica

    /// <summary>
    /// Determina si se ha consumido todo el fuente.
    /// </summary>
    private bool IsAtEnd() => _position >= _length;

    /// <summary>
    /// Retorna el carácter actual sin avanzar la posición.
    /// </summary>
    private char Peek() => IsAtEnd() ? '\0' : _source[_position];

    /// <summary>
    /// Retorna el carácter siguiente al actual sin avanzar.
    /// Útil para lookahead de 2 caracteres (ej. >=, <=, ==, !=, >>, <<).
    /// </summary>
    private char PeekNext() => (_position + 1 >= _length) ? '\0' : _source[_position + 1];

    /// <summary>
    /// Consume y retorna el carácter actual, avanzando la posición,
    /// línea y columna según corresponda.
    /// </summary>
    private char Advance()
    {
        if (IsAtEnd())
            return '\0';

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
    /// Descarta secuencias de espacios en blanco y comentarios (línea y bloque).
    /// Según la especificación TM §2.3, el whitespace NO es significativo;
    /// según §2.2, los comentarios deben ignorarse completamente.
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
    /// Consume un comentario de línea completo. Según §2.2 de la especificación,
    /// todo desde '#' hasta el fin de línea física es ignorado.
    /// </summary>
    private void SkipLineComment()
    {
        // Consumir el '#'
        Advance();
        while (!IsAtEnd() && Peek() != '\n')
        {
            Advance();
        }
        // El '\n' se deja para que SkipWhitespace lo consuma en la siguiente iteración,
        // manteniendo el conteo correcto de líneas.
    }

    /// <summary>
    /// Consume un comentario de bloque ## ... ##. Según §2.2, no pueden anidarse
    /// y terminan en el primer par '##' encontrado.
    /// </summary>
    /// <exception cref="LexerException">
    /// Si se alcanza EOF sin encontrar el cierre del comentario de bloque.
    /// </exception>
    private void SkipBlockComment()
    {
        // Consumir los dos '#'
        Advance(); // primer #
        Advance(); // segundo #

        while (!IsAtEnd())
        {
            if (Peek() == '#' && PeekNext() == '#')
            {
                Advance(); // primer #
                Advance(); // segundo #
                return;
            }
            Advance();
        }

        throw new LexerException(
            "Comentario de bloque no cerrado. Se esperaba '##' antes del fin de archivo.",
            _fileName, _line, _column);
    }

    /// <summary>
    /// Punto de despacho principal: examina el carácter actual y delega
    /// al método especializado de reconocimiento de token correspondiente.
    /// Aplica el principio Open/Closed (OCP): para agregar un nuevo token,
    /// basta extender el switch sin modificar la estructura general.
    /// </summary>
    private Token ReadNextToken()
    {
        char c = Peek();
        int startLine = _line;
        int startColumn = _column;

        // Identificadores y palabras reservadas
        if (IsLetter(c))
            return ReadIdentifierOrKeyword(startLine, startColumn);

        // Literales numéricos (enteros y flotantes)
        if (char.IsDigit(c))
            return ReadNumericLiteral(startLine, startColumn);

        // Literales de cadena (phrase)
        if (c == '"')
            return ReadStringLiteral(startLine, startColumn);

        // Literales de carácter (tone)
        if (c == '\'')
            return ReadCharLiteral(startLine, startColumn);

        // Operadores y símbolos de puntuación de 1 o 2 caracteres
        return ReadOperatorOrPunctuation(c, startLine, startColumn);
    }

    // RECONOCEDORES ESPECÍFICOS DE TOKENS

    /// <summary>
    /// Reconoce identificadores y palabras reservadas.
    /// EBNF: identifier = letter, { letter | digit | "_" };
    /// Distingue mayúsculas/minúsculas. No puede iniciar con dígito ni '_'.
    /// </summary>
    private Token ReadIdentifierOrKeyword(int startLine, int startColumn)
    {
        var lexemeBuilder = new StringBuilder();

        while (!IsAtEnd() && IsIdentifierPart(Peek()))
        {
            lexemeBuilder.Append(Advance());
        }

        string lexeme = lexemeBuilder.ToString();

        // Verificar si es palabra reservada (lookup O(1) en diccionario estático)
        if (ReservedWords.TryGetValue(lexeme, out TokenType keywordType))
        {
            return new Token(keywordType, lexeme, startLine, startColumn);
        }

        // De lo contrario, es un identificador de usuario
        return new Token(TokenType.Identifier, lexeme, startLine, startColumn);
    }

    /// <summary>
    /// Reconoce literales numéricos: enteros (sample) y flotantes (wave).
    /// EBNF:
    ///   integer_literal = digit, { digit };
    ///   float_literal   = digit, { digit }, ".", digit, { digit };
    /// El signo menos NO forma parte del literal: se reconoce como operador
    /// Minus independiente y se resuelve como unario/binario en el parser.
    /// Este método solo se invoca desde ReadNextToken() cuando el carácter
    /// actual ya es un dígito, por lo que no hace falta contemplar '-' aquí.
    /// </summary>
    private Token ReadNumericLiteral(int startLine, int startColumn)
    {
        var lexemeBuilder = new StringBuilder();
        bool isFloat = false;

        // Secuencia de dígitos obligatoria antes del punto
        if (!char.IsDigit(Peek()))
        {
            throw new LexerException(
                "Se esperaba al menos un dígito en el literal numérico.",
                _fileName, _line, _column);
        }

        while (!IsAtEnd() && char.IsDigit(Peek()))
        {
            lexemeBuilder.Append(Advance());
        }

        // Parte decimal (wave)
        if (Peek() == '.' && char.IsDigit(PeekNext()))
        {
            isFloat = true;
            lexemeBuilder.Append(Advance()); // consume '.'

            while (!IsAtEnd() && char.IsDigit(Peek()))
            {
                lexemeBuilder.Append(Advance());
            }
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
                    "Literal de cadena no cerrado. Las cadenas no pueden abarcar múltiples líneas.",
                    _fileName, _line, _column);
            }

            if (c == '\\')
            {
                Advance(); // consume '\'
                if (IsAtEnd())
                {
                    throw new LexerException(
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
                "Literal de carácter incompleto al final del archivo.",
                _fileName, _line, _column);
        }

        char value;
        char c = Peek();

        if (c == '\n')
        {
            throw new LexerException(
                "Literal de carácter no puede contener nueva línea.",
                _fileName, _line, _column);
        }

        if (c == '\\')
        {
            Advance(); // consume '\'
            if (IsAtEnd())
            {
                throw new LexerException(
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
                "Literal de carácter no cerrado o contiene múltiples caracteres. Se esperaba \"'\".",
                _fileName, _line, _column);
        }

        Advance(); // consumir comilla simple de cierre '
        return new Token(TokenType.CharLiteral, value.ToString(), startLine, startColumn);
    }

    /// <summary>
    /// Reconoce operadores, delimitadores y símbolos de puntuación.
    /// Implementa lookahead de 1 o 2 caracteres para distinguir
    /// operadores compuestos (==, !=, >=, <=, >>, <<, ++, --).
    /// </summary>
    private Token ReadOperatorOrPunctuation(char c, int startLine, int startColumn)
    {
        // Lookahead de 2 caracteres para operadores compuestos
        char next = PeekNext();

        switch (c)
        {
            // Operadores de dos caracteres (prioridad alta)
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

            // '>' y '<' sueltos son siempre relacionales a nivel léxico (spec §3.2.1).
            // Actúan como delimitadores de bloque solo en ciertas posiciones sintácticas
            // (después de "session", de la condición de if/loop/repeat, etc.) — esa
            // distinción depende de la posición en la gramática, no del carácter en sí,
            // así que la resuelve el Parser al consumir un GreaterThan/LessThan donde
            // la gramática espera un bloque, no el Lexer.
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
                    $"Carácter inesperado '{c}' (U+{(int)c:X4}). No forma parte del alfabeto léxico de TM.",
                    _fileName, startLine, startColumn);
        }
    }

    // UTILIDADES LÉXICAS PRIVADAS

    /// <summary>
    /// Determina si un carácter puede iniciar un identificador (letra ASCII).
    /// EBNF: letter = "a".."z" | "A".."Z";
    /// Nota: TM es sensible a mayúsculas/minúsculas y solo permite ASCII
    /// para inicio de identificador según la especificación.
    /// </summary>
    private static bool IsLetter(char c) =>
        (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

    /// <summary>
    /// Determina si un carácter puede aparecer en la continuación de un identificador.
    /// EBNF: letter | digit | "_"
    /// </summary>
    private static bool IsIdentifierPart(char c) =>
        IsLetter(c) || char.IsDigit(c) || c == '_';
}