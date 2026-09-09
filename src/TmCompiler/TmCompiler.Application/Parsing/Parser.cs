using TmCompiler.Domain.Lexical;
using TmCompiler.Domain.Syntactic;

namespace TmCompiler.Application.Parsing;

/// <summary>
/// Implementación del analizador sintáctico del lenguaje TM.
/// Recibe los tokens producidos por el Lexer y construye el AST
/// siguiendo las reglas sintácticas de TM.
/// </summary>
public sealed class Parser : IParser
{
    private readonly List<Token> _tokens;
    private readonly string _fileName;

    private int _position;
    private bool _parsingMixControlExpression;

    private static bool IsTypeStart(TokenType tokenType)
    {
        return IsPrimitiveType(tokenType) ||
            tokenType == TokenType.Sequence ||
            tokenType == TokenType.Score;
    }

    /// <summary>
    /// Inicializa el Parser con la secuencia de tokens generada por el Lexer.
    /// </summary>
    public Parser(IEnumerable<Token> tokens, string fileName)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        _tokens = tokens.ToList();

        if (_tokens.Count == 0)
        {
            throw new ArgumentException(
                "La secuencia de tokens no puede estar vacía.",
                nameof(tokens));
        }

        _fileName = fileName ?? "<unknown>";
        _position = 0;
    }

    /// <summary>
    /// Analiza el programa completo y retorna el nodo raíz del AST.
    /// </summary>
    public ProgramNode Parse()
    {
        var imports = new List<ImportNode>();
        var functions = new List<FunctionDeclarationNode>();

        while (Check(TokenType.Import))
        {
            imports.Add(ParseImportStatement());
        }

        while (!Check(TokenType.Session) &&
            !Check(TokenType.EndOfFile))
        {
            if (IsFunctionReturnType(Current.TokenType))
            {
                functions.Add(ParseFunctionDeclaration());
                continue;
            }

            throw new ParserException(
                $"Se esperaba una declaración de función, 'session' o fin de archivo, pero se encontró '{Current.Lexeme}'.",
                _fileName,
                Current.Line,
                Current.Column);
        }

        SessionNode session = null;

        if (Check(TokenType.Session))
        {
            session = ParseSession();
        }

        Expect(
            TokenType.EndOfFile,
            "Se esperaba el fin del archivo.");

        return new ProgramNode(
            imports,
            functions,
            session);
    }

    /// <summary>
    /// function_decl =
    ///     type, ":", "compose", identifier,
    ///     "(", [ param_list ], ")", block ;
    ///
    /// Ejemplo:
    /// sample : compose sumar(sample a, sample b) >
    ///     ret a + b;
    /// <
    /// </summary>
    private FunctionDeclarationNode ParseFunctionDeclaration()
    {
        Token startToken = Current;

        string returnType;

        if (Check(TokenType.Void))
        {
            returnType = Advance().Lexeme;
        }
        else
        {
            returnType = ParseTypeName();
        }

        Expect(
            TokenType.Colon,
            "Se esperaba ':' después del tipo de retorno.");

        Expect(
            TokenType.Compose,
            "Se esperaba la palabra reservada 'compose'.");

        Token identifier = Expect(
            TokenType.Identifier,
            "Se esperaba el nombre de la función después de 'compose'.");

        Expect(
            TokenType.LeftParen,
            "Se esperaba '(' después del nombre de la función.");

        var parameters = new List<ParameterNode>();

        if (!Check(TokenType.RightParen))
        {
            parameters.Add(ParseParameter());

            while (Match(TokenType.Comma))
            {
                parameters.Add(ParseParameter());
            }
        }

        Expect(
            TokenType.RightParen,
            "Se esperaba ')' después de los parámetros de la función.");

        BlockNode body = ParseBlock();

        return new FunctionDeclarationNode(
            returnType,
            identifier.Lexeme,
            parameters,
            body,
            startToken.Line,
            startToken.Column);
    }

    /// <summary>
    /// import_stmt = "import", string_literal, ";" ;
    ///
    /// Ejemplo:
    /// import "utilidades.tm";
    /// </summary>
    private ImportNode ParseImportStatement()
    {
        Token importToken = Expect(
            TokenType.Import,
            "Se esperaba la palabra reservada 'import'.");

        Token filePath = Expect(
            TokenType.StringLiteral,
            "Se esperaba una ruta de archivo después de 'import'.");

        Expect(
            TokenType.Semicolon,
            "Se esperaba ';' después de la importación.");

        return new ImportNode(
            filePath.Lexeme,
            importToken.Line,
            importToken.Column);
    }

    /// <summary>
    /// parameter = type, identifier ;
    /// </summary>
    private ParameterNode ParseParameter()
    {
        Token startToken = Current;

        string typeName = ParseTypeName();

        Token identifier = Expect(
            TokenType.Identifier,
            "Se esperaba el nombre del parámetro.");

        return new ParameterNode(
            typeName,
            identifier.Lexeme,
            startToken.Line,
            startToken.Column);
    }

    /// <summary>
    /// session = "session", block ;
    /// </summary>
    private SessionNode ParseSession()
    {
        Expect(
            TokenType.Session,
            "Se esperaba la palabra reservada 'session'.");

        BlockNode body = ParseBlock();

        return new SessionNode(body);
    }

    /// <summary>
    /// block = ">", { statement }, "<" ;
    /// </summary>
    private BlockNode ParseBlock()
    {
        Expect(
            TokenType.GreaterThan,
            "Se esperaba '>' para iniciar el bloque.");

        var statements = new List<StatementNode>();

        while (!Check(TokenType.LessThan) &&
               !Check(TokenType.EndOfFile))
        {
            statements.Add(ParseStatement());
        }

        Expect(
            TokenType.LessThan,
            "Se esperaba '<' para finalizar el bloque.");

        return new BlockNode(statements);
    }

    /// <summary>
    /// Determina qué tipo de sentencia comienza en el token actual
    /// y delega su análisis al método correspondiente.
    /// </summary>
    private StatementNode ParseStatement()
    {
        if (Check(TokenType.Const))
        {
            return ParseConstantDeclaration();
        }

        if (IsTypeStart(Current.TokenType))
        {
            return ParseVariableDeclaration();
        }

        if (Check(TokenType.Identifier) &&
            CheckNext(TokenType.Assign))
        {
            return ParseAssignmentStatement();
        }

        if (Check(TokenType.Identifier) &&
            (CheckNext(TokenType.Increment) ||
            CheckNext(TokenType.Decrement)))
        {
            return ParseUpdateStatement();
        }

        if (Check(TokenType.Identifier) &&
            CheckNext(TokenType.LeftBracket))
        {
            return ParseIndexedAssignmentStatement();
        }

        if (Check(TokenType.Identifier) &&
            CheckNext(TokenType.LeftParen))
        {
            return ParseExpressionStatement();
        }

        if (Check(TokenType.Output))
        {
            return ParseOutputStatement();
        }

        if (Check(TokenType.Input))
        {
            return ParseInputStatement();
        }

        if (Check(TokenType.Ret))
        {
            return ParseReturnStatement();
        }

        if (Check(TokenType.If))
        {
            return ParseIfStatement();
        }

        if (Check(TokenType.Repeat))
        {
            return ParseRepeatStatement();
        }

        if (Check(TokenType.Loop))
        {
            return ParseLoopStatement();
        }

        if (Check(TokenType.Mix))
        {
            return ParseMixStatement();
        }

        throw new ParserException(
            $"Sentencia inesperada '{Current.Lexeme}'.",
            _fileName,
            Current.Line,
            Current.Column);
    }

    /// <summary>
    /// var_decl = type, identifier, "=", expression, ";" ;
    ///
    /// Ejemplos:
    /// sample edad = 22;
    /// sequence<sample> numeros = [1, 2, 3];
    /// score<sample> matriz = [[1, 2], [3, 4]];
    /// </summary>
    private VariableDeclarationNode ParseVariableDeclaration()
    {
        Token startToken = Current;

        string typeName = ParseTypeName();

        Token identifier = Expect(
            TokenType.Identifier,
            "Se esperaba un identificador después del tipo de la variable.");

        Expect(
            TokenType.Assign,
            "Se esperaba '=' después del identificador de la variable.");

        ExpressionNode initializer = ParseExpression();

        Expect(
            TokenType.Semicolon,
            "Se esperaba ';' al final de la declaración de variable.");

        return new VariableDeclarationNode(
            typeName,
            identifier.Lexeme,
            initializer,
            startToken.Line,
            startToken.Column);
    }

    /// <summary>
    /// const_decl = "const", type, identifier, "=", expression, ";" ;
    ///
    /// Ejemplo:
    /// const sample MAX = 100;
    /// const sequence<sample> NUMEROS = [1, 2, 3];
    /// </summary>
    private ConstantDeclarationNode ParseConstantDeclaration()
    {
        Token constToken = Expect(
            TokenType.Const,
            "Se esperaba la palabra reservada 'const'.");

        string typeName = ParseTypeName();

        Token identifier = Expect(
            TokenType.Identifier,
            "Se esperaba un identificador después del tipo de la constante.");

        Expect(
            TokenType.Assign,
            "Se esperaba '=' después del identificador de la constante.");

        ExpressionNode initializer = ParseExpression();

        Expect(
            TokenType.Semicolon,
            "Se esperaba ';' al final de la declaración de constante.");

        return new ConstantDeclarationNode(
            typeName,
            identifier.Lexeme,
            initializer,
            constToken.Line,
            constToken.Column);
    }

    /// <summary>
    /// assign_stmt = identifier, "=", expression, ";" ;
    ///
    /// Ejemplo:
    /// x = x + 1;
    /// </summary>
    private AssignmentStatementNode ParseAssignmentStatement()
    {
        Token identifier = Expect(
            TokenType.Identifier,
            "Se esperaba un identificador al inicio de la asignación.");

        Expect(
            TokenType.Assign,
            "Se esperaba '=' después del identificador.");

        ExpressionNode value = ParseExpression();

        Expect(
            TokenType.Semicolon,
            "Se esperaba ';' al final de la asignación.");

        return new AssignmentStatementNode(
            identifier.Lexeme,
            value,
            identifier.Line,
            identifier.Column);
    }

    /// <summary>
    /// Analiza una actualización unitaria como sentencia.
    ///
    /// Ejemplos:
    /// contador++;
    /// contador--;
    /// </summary>
    private UpdateStatementNode ParseUpdateStatement()
    {
        Token identifier = Expect(
            TokenType.Identifier,
            "Se esperaba un identificador.");

        Token operatorToken = Current;

        if (!Match(TokenType.Increment, TokenType.Decrement))
        {
            throw new ParserException(
                "Se esperaba '++' o '--' después del identificador.",
                _fileName,
                Current.Line,
                Current.Column);
        }

        Expect(
            TokenType.Semicolon,
            "Se esperaba ';' después de la actualización.");

        return new UpdateStatementNode(
            identifier.Lexeme,
            operatorToken.TokenType,
            identifier.Line,
            identifier.Column);
    }

    /// <summary>
    /// Analiza una asignación indexada.
    ///
    /// Ejemplos:
    /// numeros[i] = 10;
    /// matriz[i][j] = 20;
    /// </summary>
    private IndexedAssignmentStatementNode ParseIndexedAssignmentStatement()
    {
        Token startToken = Current;

        ExpressionNode target = ParsePrimaryExpression();

        if (target is not IndexExpressionNode indexTarget)
        {
            throw new ParserException(
                "Se esperaba un acceso indexado en la asignación.",
                _fileName,
                startToken.Line,
                startToken.Column);
        }

        Expect(
            TokenType.Assign,
            "Se esperaba '=' después del acceso indexado.");

        ExpressionNode value = ParseExpression();

        Expect(
            TokenType.Semicolon,
            "Se esperaba ';' al final de la asignación indexada.");

        return new IndexedAssignmentStatementNode(
            indexTarget,
            value,
            startToken.Line,
            startToken.Column);
    }

    /// <summary>
    /// Representa una expresión utilizada como sentencia.
    /// Actualmente se utiliza principalmente para llamadas a funciones.
    ///
    /// Ejemplo:
    /// saludar("Ana");
    /// </summary>
    private ExpressionStatementNode ParseExpressionStatement()
    {
        Token startToken = Current;

        ExpressionNode expression = ParseExpression();

        if (expression is not CallExpressionNode)
        {
            throw new ParserException(
                "Solo se permiten llamadas a funciones como sentencias de expresión.",
                _fileName,
                startToken.Line,
                startToken.Column);
        }

        Expect(
            TokenType.Semicolon,
            "Se esperaba ';' después de la llamada a función.");

        return new ExpressionStatementNode(
            expression,
            startToken.Line,
            startToken.Column);
    }

    /// <summary>
    /// output_stmt = "output", ">>", expression, ";" ;
    /// </summary>
    private OutputStatementNode ParseOutputStatement()
    {
        Token outputToken = Expect(
            TokenType.Output,
            "Se esperaba la palabra reservada 'output'.");

        Expect(
            TokenType.OutputOp,
            "Se esperaba '>>' después de 'output'.");

        ExpressionNode expression = ParseExpression();

        Expect(
            TokenType.Semicolon,
            "Se esperaba ';' al final de la sentencia output.");

        return new OutputStatementNode(
            expression,
            outputToken.Line,
            outputToken.Column);
    }

    /// <summary>
    /// input_stmt = "input", "<<", identifier, ";" ;
    /// </summary>
    private InputStatementNode ParseInputStatement()
    {
        Token inputToken = Expect(
            TokenType.Input,
            "Se esperaba la palabra reservada 'input'.");

        Expect(
            TokenType.InputOp,
            "Se esperaba '<<' después de 'input'.");

        Token identifier = Expect(
            TokenType.Identifier,
            "Se esperaba un identificador después de 'input <<'.");

        Expect(
            TokenType.Semicolon,
            "Se esperaba ';' al final de la sentencia input.");

        return new InputStatementNode(
            identifier.Lexeme,
            inputToken.Line,
            inputToken.Column);
    }

    /// <summary>
    /// ret_stmt = "ret", [ expression ], ";" ;
    ///
    /// Ejemplos:
    /// ret;
    /// ret resultado;
    /// ret a + b;
    /// </summary>
    private ReturnStatementNode ParseReturnStatement()
    {
        Token retToken = Expect(
            TokenType.Ret,
            "Se esperaba la palabra reservada 'ret'.");

        ExpressionNode expression = null;

        // Si el siguiente token no es ';',
        // entonces ret contiene una expresión.
        if (!Check(TokenType.Semicolon))
        {
            expression = ParseExpression();
        }

        Expect(
            TokenType.Semicolon,
            "Se esperaba ';' al final de la sentencia ret.");

        return new ReturnStatementNode(
            expression,
            retToken.Line,
            retToken.Column);
    }

    /// <summary>
    /// if_statement =
    ///     "if", "[", expression, "]", block,
    ///     { "else", "if", "[", expression, "]", block },
    ///     [ "else", block ] ;
    ///
    /// Ejemplos:
    ///
    /// if [x > 10] > ... <
    ///
    /// if [x > 10] > ... <
    /// else > ... <
    ///
    /// if [x > 10] > ... <
    /// else if [x == 10] > ... <
    /// else > ... <
    /// </summary>
    private IfStatementNode ParseIfStatement()
    {
        Token ifToken = Expect(
            TokenType.If,
            "Se esperaba la palabra reservada 'if'.");

        Expect(
            TokenType.LeftBracket,
            "Se esperaba '[' después de 'if'.");

        ExpressionNode condition = ParseExpression();

        Expect(
            TokenType.RightBracket,
            "Se esperaba ']' después de la condición del if.");

        BlockNode thenBlock = ParseBlock();

        var elseIfClauses = new List<ElseIfClauseNode>();

        // else if [...]
        while (Check(TokenType.Else) && CheckNext(TokenType.If))
        {
            Expect(
                TokenType.Else,
                "Se esperaba 'else'.");

            Expect(
                TokenType.If,
                "Se esperaba 'if' después de 'else'.");

            Expect(
                TokenType.LeftBracket,
                "Se esperaba '[' después de 'else if'.");

            ExpressionNode elseIfCondition = ParseExpression();

            Expect(
                TokenType.RightBracket,
                "Se esperaba ']' después de la condición de 'else if'.");

            BlockNode elseIfBlock = ParseBlock();

            elseIfClauses.Add(
                new ElseIfClauseNode(
                    elseIfCondition,
                    elseIfBlock));
        }

        // else opcional
        BlockNode elseBlock = null;

        if (Check(TokenType.Else))
        {
            Advance();

            elseBlock = ParseBlock();
        }

        return new IfStatementNode(
            condition,
            thenBlock,
            elseIfClauses,
            elseBlock,
            ifToken.Line,
            ifToken.Column);
    }

    /// <summary>
    /// repeat_statement = "repeat", "[", expression, "]", block ;
    ///
    /// Ejemplo:
    /// repeat [x < 10] >
    ///     x = x + 1;
    /// <
    /// </summary>
    private RepeatStatementNode ParseRepeatStatement()
    {
        Token repeatToken = Expect(
            TokenType.Repeat,
            "Se esperaba la palabra reservada 'repeat'.");

        Expect(
            TokenType.LeftBracket,
            "Se esperaba '[' después de 'repeat'.");

        ExpressionNode condition = ParseExpression();

        Expect(
            TokenType.RightBracket,
            "Se esperaba ']' después de la condición de repeat.");

        BlockNode body = ParseBlock();

        return new RepeatStatementNode(
            condition,
            body,
            repeatToken.Line,
            repeatToken.Column);
    }

    /// <summary>
    /// loop_statement = "loop", "[",
    ///     var_decl, expression, ";", update_expr,
    ///     "]", block ;
    ///
    /// Ejemplo:
    /// loop [sample i = 0; i < 5; i++] >
    ///     output >> i;
    /// <
    /// </summary>
    private LoopStatementNode ParseLoopStatement()
    {
        Token loopToken = Expect(
            TokenType.Loop,
            "Se esperaba la palabra reservada 'loop'.");

        Expect(
            TokenType.LeftBracket,
            "Se esperaba '[' después de 'loop'.");

        if (!IsTypeStart(Current.TokenType))
        {
            throw new ParserException(
                "Se esperaba una declaración de variable al inicio del loop.",
                _fileName,
                Current.Line,
                Current.Column);
        }

        VariableDeclarationNode initializer =
            ParseVariableDeclaration();

        ExpressionNode condition = ParseExpression();

        Expect(
            TokenType.Semicolon,
            "Se esperaba ';' después de la condición del loop.");

        LoopUpdateNode update = ParseLoopUpdate();

        Expect(
            TokenType.RightBracket,
            "Se esperaba ']' después de la actualización del loop.");

        BlockNode body = ParseBlock();

        return new LoopStatementNode(
            initializer,
            condition,
            update,
            body,
            loopToken.Line,
            loopToken.Column);
    }

    /// <summary>
    /// update_expr =
    ///     identifier, ( "++" | "--" )
    ///     | identifier, "=", expression ;
    /// </summary>
    private LoopUpdateNode ParseLoopUpdate()
    {
        Token identifier = Expect(
            TokenType.Identifier,
            "Se esperaba un identificador en la actualización del loop.");

        if (Match(TokenType.Increment, TokenType.Decrement))
        {
            Token operatorToken = Previous;

            return new LoopUpdateNode(
                identifier.Lexeme,
                operatorToken.TokenType,
                null,
                identifier.Line,
                identifier.Column);
        }

        if (Match(TokenType.Assign))
        {
            ExpressionNode value = ParseExpression();

            return new LoopUpdateNode(
                identifier.Lexeme,
                TokenType.Assign,
                value,
                identifier.Line,
                identifier.Column);
        }

        throw new ParserException(
            "Se esperaba '++', '--' o '=' en la actualización del loop.",
            _fileName,
            Current.Line,
            Current.Column);
    }

    /// <summary>
    /// Analiza una sentencia mix usando bloques delimitados por > y <.
    ///
    /// Ejemplo:
    /// mix opcion >
    ///     case 1: >
    ///         output >> "Uno";
    ///     <
    ///     default: >
    ///         output >> "Otro";
    ///     <
    /// <
    /// </summary>
    private MixStatementNode ParseMixStatement()
    {
        Token mixToken = Expect(
            TokenType.Mix,
            "Se esperaba la palabra reservada 'mix'.");

        ExpressionNode expression;

        _parsingMixControlExpression = true;

        try
        {
            expression = ParseExpression();
        }
        finally
        {
            _parsingMixControlExpression = false;
        }

        Expect(
            TokenType.GreaterThan,
            "Se esperaba '>' para iniciar el bloque de mix.");

        var cases = new List<MixCaseNode>();
        BlockNode defaultBlock = null;

        while (!Check(TokenType.LessThan) &&
            !Check(TokenType.EndOfFile))
        {
            if (Check(TokenType.Identifier) &&
                string.Equals(Current.Lexeme, "case", StringComparison.Ordinal))
            {
                cases.Add(ParseMixCase());
                continue;
            }

            if (Check(TokenType.Identifier) &&
                string.Equals(Current.Lexeme, "default", StringComparison.Ordinal))
            {
                if (defaultBlock is not null)
                {
                    throw new ParserException(
                        "La sentencia mix no puede contener más de un bloque default.",
                        _fileName,
                        Current.Line,
                        Current.Column);
                }

                defaultBlock = ParseMixDefault();
                continue;
            }

            throw new ParserException(
                $"Se esperaba 'case', 'default' o '<' dentro de mix, pero se encontró '{Current.Lexeme}'.",
                _fileName,
                Current.Line,
                Current.Column);
        }

        Expect(
            TokenType.LessThan,
            "Se esperaba '<' para finalizar la sentencia mix.");

        return new MixStatementNode(
            expression,
            cases,
            defaultBlock,
            mixToken.Line,
            mixToken.Column);
    }

    /// <summary>
    /// Analiza una rama case dentro de mix.
    ///
    /// case expresion: > ... <
    /// </summary>
    private MixCaseNode ParseMixCase()
    {
        Token caseToken = Current;

        if (caseToken.TokenType != TokenType.Identifier ||
            !string.Equals(caseToken.Lexeme, "case", StringComparison.Ordinal))
        {
            throw new ParserException(
                "Se esperaba 'case'.",
                _fileName,
                caseToken.Line,
                caseToken.Column);
        }

        Advance();

        ExpressionNode value = ParseExpression();

        Expect(
            TokenType.Colon,
            "Se esperaba ':' después del valor de case.");

        BlockNode body = ParseBlock();

        return new MixCaseNode(
            value,
            body);
    }

    /// <summary>
    /// Analiza el bloque default opcional dentro de mix.
    ///
    /// default: > ... <
    /// </summary>
    private BlockNode ParseMixDefault()
    {
        Token defaultToken = Current;

        if (defaultToken.TokenType != TokenType.Identifier ||
            !string.Equals(defaultToken.Lexeme, "default", StringComparison.Ordinal))
        {
            throw new ParserException(
                "Se esperaba 'default'.",
                _fileName,
                defaultToken.Line,
                defaultToken.Column);
        }

        Advance();

        Expect(
            TokenType.Colon,
            "Se esperaba ':' después de 'default'.");

        return ParseBlock();
    }

    // ============================================================
    // EXPRESIONES
    // ============================================================

    /// <summary>
    /// expression = or_expr ;
    /// </summary>
    private ExpressionNode ParseExpression()
    {
        return ParseOrExpression();
    }

    /// <summary>
    /// or_expr = and_expr, { "or", and_expr } ;
    /// </summary>
    private ExpressionNode ParseOrExpression()
    {
        ExpressionNode expression = ParseAndExpression();

        while (Match(TokenType.Or))
        {
            Token operatorToken = Previous;
            ExpressionNode right = ParseAndExpression();

            expression = new BinaryExpressionNode(
                expression,
                operatorToken.TokenType,
                right,
                operatorToken.Line,
                operatorToken.Column);
        }

        return expression;
    }

    /// <summary>
    /// and_expr = equality_expr, { "and", equality_expr } ;
    /// </summary>
    private ExpressionNode ParseAndExpression()
    {
        ExpressionNode expression = ParseEqualityExpression();

        while (Match(TokenType.And))
        {
            Token operatorToken = Previous;
            ExpressionNode right = ParseEqualityExpression();

            expression = new BinaryExpressionNode(
                expression,
                operatorToken.TokenType,
                right,
                operatorToken.Line,
                operatorToken.Column);
        }

        return expression;
    }

    /// <summary>
    /// equality_expr =
    ///     relational_expr,
    ///     { ( "==" | "!=" ), relational_expr } ;
    /// </summary>
    private ExpressionNode ParseEqualityExpression()
    {
        ExpressionNode expression = ParseRelationalExpression();

        while (Match(TokenType.Equal, TokenType.NotEqual))
        {
            Token operatorToken = Previous;
            ExpressionNode right = ParseRelationalExpression();

            expression = new BinaryExpressionNode(
                expression,
                operatorToken.TokenType,
                right,
                operatorToken.Line,
                operatorToken.Column);
        }

        return expression;
    }

    /// <summary>
    /// relational_expr =
    ///     additive_expr,
    ///     { ( ">" | "<" | ">=" | "<=" ), additive_expr } ;
    /// </summary>
    private ExpressionNode ParseRelationalExpression()
    {
        ExpressionNode expression = ParseAdditiveExpression();

        while (!IsMixBlockStart() &&
            Match(
                TokenType.GreaterThan,
                TokenType.LessThan,
                TokenType.GreaterOrEqual,
                TokenType.LessOrEqual))
        {
            Token operatorToken = Previous;
            ExpressionNode right = ParseAdditiveExpression();

            expression = new BinaryExpressionNode(
                expression,
                operatorToken.TokenType,
                right,
                operatorToken.Line,
                operatorToken.Column);
        }

        return expression;
    }

    /// <summary>
    /// additive_expr =
    ///     multiplicative_expr,
    ///     { ( "+" | "-" ), multiplicative_expr } ;
    /// </summary>
    private ExpressionNode ParseAdditiveExpression()
    {
        ExpressionNode expression = ParseMultiplicativeExpression();

        while (Match(TokenType.Plus, TokenType.Minus))
        {
            Token operatorToken = Previous;
            ExpressionNode right = ParseMultiplicativeExpression();

            expression = new BinaryExpressionNode(
                expression,
                operatorToken.TokenType,
                right,
                operatorToken.Line,
                operatorToken.Column);
        }

        return expression;
    }

    /// <summary>
    /// multiplicative_expr =
    ///     unary_expr,
    ///     { ( "*" | "/" ), unary_expr } ;
    /// </summary>
    private ExpressionNode ParseMultiplicativeExpression()
    {
        ExpressionNode expression = ParseUnaryExpression();

        while (Match(TokenType.Multiply, TokenType.Divide))
        {
            Token operatorToken = Previous;
            ExpressionNode right = ParseUnaryExpression();

            expression = new BinaryExpressionNode(
                expression,
                operatorToken.TokenType,
                right,
                operatorToken.Line,
                operatorToken.Column);
        }

        return expression;
    }

    /// <summary>
    /// unary_expr =
    ///     ( "not" | "-" ), unary_expr
    ///     | primary_expr ;
    /// </summary>
    private ExpressionNode ParseUnaryExpression()
    {
        if (Match(TokenType.Not, TokenType.Minus))
        {
            Token operatorToken = Previous;
            ExpressionNode operand = ParseUnaryExpression();

            return new UnaryExpressionNode(
                operatorToken.TokenType,
                operand,
                operatorToken.Line,
                operatorToken.Column);
        }

        return ParsePrimaryExpression();
    }

    /// <summary>
    /// Analiza una expresión primaria y sus operaciones postfix:
    /// indexación y length().
    ///
    /// Ejemplos:
    /// numeros[0]
    /// matriz[i][j]
    /// numeros.length()
    /// </summary>
    private ExpressionNode ParsePrimaryExpression()
    {
        ExpressionNode expression = ParseAtomicExpression();

        while (true)
        {
            // Indexación: expresion[indice]
            if (Match(TokenType.LeftBracket))
            {
                Token bracket = Previous;

                ExpressionNode index = ParseExpression();

                Expect(
                    TokenType.RightBracket,
                    "Se esperaba ']' después del índice.");

                expression = new IndexExpressionNode(
                    expression,
                    index,
                    bracket.Line,
                    bracket.Column);

                continue;
            }

            // Propiedad .length()
            if (Match(TokenType.Dot))
            {
                Token dotToken = Previous;

                Token member = Expect(
                    TokenType.Identifier,
                    "Se esperaba un miembro después de '.'.");

                if (!string.Equals(
                        member.Lexeme,
                        "length",
                        StringComparison.Ordinal))
                {
                    throw new ParserException(
                        $"Propiedad no reconocida '{member.Lexeme}'.",
                        _fileName,
                        member.Line,
                        member.Column);
                }

                Expect(
                    TokenType.LeftParen,
                    "Se esperaba '(' después de 'length'.");

                Expect(
                    TokenType.RightParen,
                    "Se esperaba ')' después de 'length('.");

                expression = new LengthExpressionNode(
                    expression,
                    dotToken.Line,
                    dotToken.Column);

                continue;
            }

            break;
        }

        return expression;
    }

    /// <summary>
    /// Analiza los elementos básicos de una expresión:
    /// literales, identificadores y expresiones entre paréntesis.
    /// </summary>
    private ExpressionNode ParseAtomicExpression()
    {
        Token token = Current;

        // Literales
        if (IsLiteral(token.TokenType))
        {
            Advance();

            return new LiteralExpressionNode(
                token.TokenType,
                token.Lexeme,
                token.Line,
                token.Column);
        }

        // Identificadores
        if (Match(TokenType.Identifier))
        {
            Token identifier = Previous;

            // Llamada a función: nombre(...)
            if (Match(TokenType.LeftParen))
            {
                var arguments = new List<ExpressionNode>();

                if (!Check(TokenType.RightParen))
                {
                    arguments.Add(ParseExpression());

                    while (Match(TokenType.Comma))
                    {
                        arguments.Add(ParseExpression());
                    }
                }

                Expect(
                    TokenType.RightParen,
                    "Se esperaba ')' después de los argumentos de la función.");

                return new CallExpressionNode(
                    identifier.Lexeme,
                    arguments,
                    identifier.Line,
                    identifier.Column);
            }

            return new IdentifierExpressionNode(
                identifier.Lexeme,
                identifier.Line,
                identifier.Column);
        }

        // Expresión agrupada: ( expresion )
        if (Match(TokenType.LeftParen))
        {
            ExpressionNode expression = ParseExpression();

            Expect(
                TokenType.RightParen,
                "Se esperaba ')' después de la expresión.");

            return expression;
        }

        if (Match(TokenType.LeftBracket))
        {
            Token openingBracket = Previous;

            var elements = new List<ExpressionNode>();

            if (!Check(TokenType.RightBracket))
            {
                elements.Add(ParseExpression());

                while (Match(TokenType.Comma))
                {
                    elements.Add(ParseExpression());
                }
            }

            Expect(
                TokenType.RightBracket,
                "Se esperaba ']' después de los elementos de la colección.");

            return new CollectionLiteralExpressionNode(
                elements,
                openingBracket.Line,
                openingBracket.Column);
        }

        // Literal de colección:
        // [1, 2, 3]
        // [[1, 2], [3, 4]]
        if (Match(TokenType.LeftBracket))
        {
            Token openingBracket = Previous;

            var elements = new List<ExpressionNode>();

            if (!Check(TokenType.RightBracket))
            {
                elements.Add(ParseExpression());

                while (Match(TokenType.Comma))
                {
                    elements.Add(ParseExpression());
                }
            }

            Expect(
                TokenType.RightBracket,
                "Se esperaba ']' después de los elementos de la colección.");

            return new CollectionLiteralExpressionNode(
                elements,
                openingBracket.Line,
                openingBracket.Column);
        }

        throw new ParserException(
            $"Se esperaba una expresión, pero se encontró '{token.Lexeme}'.",
            _fileName,
            token.Line,
            token.Column);
    }

    // ============================================================
    // UTILIDADES
    // ============================================================

    /// <summary>
    /// Comprueba si el token actual coincide con alguno de los tipos
    /// proporcionados. Si coincide, lo consume.
    /// </summary>
    private bool Match(params TokenType[] tokenTypes)
    {
        foreach (TokenType tokenType in tokenTypes)
        {
            if (Check(tokenType))
            {
                Advance();
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Comprueba el token actual sin consumirlo.
    /// </summary>
    private bool Check(TokenType tokenType)
    {
        return Current.TokenType == tokenType;
    }

    /// <summary>
    /// Comprueba el siguiente token sin consumirlo.
    /// </summary>
    private bool CheckNext(TokenType tokenType)
    {
        if (_position + 1 >= _tokens.Count)
        {
            return false;
        }

        return _tokens[_position + 1].TokenType == tokenType;
    }

    /// <summary>
    /// Determina si el token '>' actual corresponde al inicio
    /// del bloque de una sentencia mix y no a un operador relacional.
    /// </summary>
    private bool IsMixBlockStart()
    {
        if (!_parsingMixControlExpression ||
            !Check(TokenType.GreaterThan))
        {
            return false;
        }

        if (_position + 1 >= _tokens.Count)
        {
            return true;
        }

        Token next = _tokens[_position + 1];

        if (next.TokenType == TokenType.LessThan)
        {
            return true;
        }

        return next.TokenType == TokenType.Identifier &&
            (string.Equals(next.Lexeme, "case", StringComparison.Ordinal) ||
                string.Equals(next.Lexeme, "default", StringComparison.Ordinal));
    }

    /// <summary>
    /// Analiza un tipo TM y retorna su nombre completo.
    ///
    /// Ejemplos:
    /// sample
    /// phrase
    /// sequence<sample>
    /// score<wave>
    /// </summary>
    private string ParseTypeName()
    {
        if (IsPrimitiveType(Current.TokenType))
        {
            return Advance().Lexeme;
        }

        if (Check(TokenType.Sequence) || Check(TokenType.Score))
        {
            Token compositeType = Advance();

            Expect(
                TokenType.LessThan,
                "Se esperaba '<' después del tipo compuesto.");

            string innerType = ParseTypeName();

            Expect(
                TokenType.GreaterThan,
                "Se esperaba '>' después del tipo interno.");

            return $"{compositeType.Lexeme}<{innerType}>";
        }

        throw new ParserException(
            $"Se esperaba un tipo válido, pero se encontró '{Current.Lexeme}'.",
            _fileName,
            Current.Line,
            Current.Column);
    }

    /// <summary>
    /// Indica si un token puede representar un tipo primitivo.
    /// </summary>
    private static bool IsPrimitiveType(TokenType tokenType)
    {
        return tokenType is
            TokenType.Sample or
            TokenType.Wave or
            TokenType.Tone or
            TokenType.Phrase or
            TokenType.Signal or
            TokenType.Silence;
    }

    private static bool IsFunctionReturnType(TokenType tokenType)
    {
        return IsTypeStart(tokenType) ||
            tokenType == TokenType.Void;
    }

    /// <summary>
    /// Indica si un token representa un literal.
    /// </summary>
    private static bool IsLiteral(TokenType tokenType)
    {
        return tokenType is
            TokenType.IntegerLiteral or
            TokenType.FloatLiteral or
            TokenType.StringLiteral or
            TokenType.CharLiteral or
            TokenType.True or
            TokenType.False or
            TokenType.Silence or
            TokenType.BoolLiteral or
            TokenType.NullLiteral;
    }

    /// <summary>
    /// Verifica que el token actual sea del tipo esperado.
    /// </summary>
    private Token Expect(TokenType expectedType, string message)
    {
        Token current = Current;

        if (current.TokenType != expectedType)
        {
            throw new ParserException(
                message,
                _fileName,
                current.Line,
                current.Column);
        }

        return Advance();
    }

    /// <summary>
    /// Token actualmente señalado por el Parser.
    /// </summary>
    private Token Current => _tokens[_position];

    /// <summary>
    /// Token consumido inmediatamente antes del token actual.
    /// </summary>
    private Token Previous => _tokens[_position - 1];

    /// <summary>
    /// Consume el token actual y avanza.
    /// </summary>
   private Token Advance()
    {
        Token current = Current;

        if (_position < _tokens.Count - 1)
        {
            _position++;
        }

        return current;
    }
}