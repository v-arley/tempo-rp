using Tempo.Language.Diagnostics;
using Tempo.Language.Symbols;
using Tempo.Language.Syntax;

namespace Tempo.Language.Binding;

/// <summary>
/// Sintetiza el tipo de una expresión y reporta los errores de tipo que
/// encuentra. Devuelve <see cref="TmType.Unknown"/> en cuanto algo falla, y
/// todo consumidor comprueba ese caso antes de reportar: así un único error de
/// origen no desencadena una cascada de errores derivados.
/// </summary>

public sealed class ExpressionTypeResolver
{
    private readonly SymbolTable _symbols;
    private readonly DiagnosticBag _diagnostics;
    private readonly HashSet<string> _unresolvedCalls = new(StringComparer.Ordinal);

    public ExpressionTypeResolver(SymbolTable symbols, DiagnosticBag diagnostics)
    {
        _symbols = symbols;
        _diagnostics = diagnostics;
    }

    /// <summary>
    /// Cuando es <c>true</c>, una llamada a función desconocida no se reporta
    /// como E001: el archivo tiene importaciones sin resolver y el nombre podría
    /// venir de otro archivo del proyecto.
    /// </summary>
    public bool HasUnresolvedImports { get; set; }

    /// <summary>
    /// Funciones de otros archivos del proyecto que este archivo no importa
    /// (nombre → archivo que la declara). Una llamada a ellas se reporta como E020.
    /// </summary>
    public IReadOnlyDictionary<string, string> UnimportedFunctions { get; set; }

    public IReadOnlyCollection<string> UnresolvedFunctionCalls => _unresolvedCalls;

    public TmType Resolve(ExpressionNode expression)
    {
        return Resolve(expression, null);
    }

    /// <summary>
    /// Resuelve la expresión conociendo el tipo que el contexto espera. Solo lo
    /// aprovechan los literales de colección, cuyo tipo de elemento no siempre
    /// puede inferirse por sí solo.
    /// </summary>
    public TmType Resolve(ExpressionNode expression, TmType expectedType)
    {
        switch (expression)
        {
            case null:
                return TmType.Unknown;

            case LiteralExpressionNode literal:
                return ResolveLiteral(literal);

            case IdentifierExpressionNode identifier:
                return ResolveIdentifier(identifier);

            case BinaryExpressionNode binary:
                return ResolveBinary(binary);

            case UnaryExpressionNode unary:
                return ResolveUnary(unary);

            case CallExpressionNode call:
                return ResolveCall(call, false);

            case IndexExpressionNode index:
                return ResolveIndex(index);

            case LengthExpressionNode length:
                return ResolveLength(length);

            case CollectionLiteralExpressionNode collection:
                return ResolveCollectionLiteral(collection, expectedType);

            default:
                return TmType.Unknown;
        }
    }

    /// <summary>
    /// Resuelve una llamada usada como sentencia, donde un retorno
    /// <c>void</c> es legítimo.
    /// </summary>
    public TmType ResolveCallStatement(CallExpressionNode call)
    {
        return ResolveCall(call, true);
    }

    private static TmType ResolveLiteral(LiteralExpressionNode literal)
    {
        switch (literal.LiteralType)
        {
            case TokenType.IntegerLiteral:
                return TmType.Sample;

            case TokenType.FloatLiteral:
                return TmType.Wave;

            case TokenType.StringLiteral:
                return TmType.Phrase;

            case TokenType.CharLiteral:
                return TmType.Tone;

            // El lexer emite las palabras reservadas, no las categorías
            // BoolLiteral/NullLiteral; se aceptan ambas por robustez.
            case TokenType.True:
            case TokenType.False:
            case TokenType.BoolLiteral:
                return TmType.Signal;

            case TokenType.Silence:
            case TokenType.NullLiteral:
                return TmType.Silence;

            default:
                return TmType.Unknown;
        }
    }

    private TmType ResolveIdentifier(IdentifierExpressionNode identifier)
    {
        VariableSymbol symbol = _symbols.ResolveVariable(identifier.Name);

        if (symbol is null)
        {
            ReportUndeclared(identifier.Name, identifier.Line);
            return TmType.Unknown;
        }

        symbol.IsUsed = true;
        return symbol.Type;
    }

    private TmType ResolveBinary(BinaryExpressionNode binary)
    {
        TmType left = Resolve(binary.Left);
        TmType right = Resolve(binary.Right);

        if (!OperatorRuleTable.TryResolveBinary(binary.Operator, left, right, out TmType result))
        {
            _diagnostics.ReportError(
                DiagnosticCodes.IncompatibleOperation,
                binary.Line,
                $"El operador '{Describe(binary.Operator)}' no está definido entre " +
                $"'{left}' y '{right}'.",
                "Revise la tabla de operadores de la especificación y use tipos compatibles.");

            return TmType.Unknown;
        }

        if (OperatorRuleTable.RequiresPhraseConcatenationWarning(binary.Operator, left, right))
        {
            TmType converted = left.Kind == TypeKind.Phrase ? right : left;

            _diagnostics.ReportWarning(
                DiagnosticCodes.ImplicitPhraseConversion,
                binary.Line,
                $"El valor de tipo '{converted}' se convierte a texto al concatenarlo " +
                "con un 'phrase'.",
                "Si la conversión es intencionada, puede ignorar esta advertencia.");
        }

        return result;
    }

    private TmType ResolveUnary(UnaryExpressionNode unary)
    {
        TmType operand = Resolve(unary.Operand);

        if (!OperatorRuleTable.TryResolveUnary(unary.Operator, operand, out TmType result))
        {
            _diagnostics.ReportError(
                DiagnosticCodes.IncompatibleOperation,
                unary.Line,
                $"El operador unario '{Describe(unary.Operator)}' no está definido para " +
                $"'{operand}'.",
                "Use 'not' sobre un 'signal' o '-' sobre un 'sample' o un 'wave'.");

            return TmType.Unknown;
        }

        return result;
    }

    private TmType ResolveCall(CallExpressionNode call, bool asStatement)
    {
        FunctionSymbol function = _symbols.ResolveFunction(call.Identifier);

        if (function is null)
        {
            // Con importaciones sin resolver, el nombre podría estar definido en
            // otro archivo: se anota en vez de reportarse.
            if (HasUnresolvedImports)
            {
                _unresolvedCalls.Add(call.Identifier);
            }
            else if (UnimportedFunctions is not null &&
                    UnimportedFunctions.TryGetValue(call.Identifier, out string declaringFile))
            {
                _diagnostics.ReportError(
                    DiagnosticCodes.UnimportedFunction,
                    call.Line,
                    $"La función '{call.Identifier}' está declarada en '{declaringFile}', " +
                    "pero ese archivo no se importa.",
                    $"Agregue import \"{declaringFile}\"; al inicio del archivo.");
            }
            else
            {
                _diagnostics.ReportError(
                    DiagnosticCodes.UndeclaredIdentifier,
                    call.Line,
                    $"La función '{call.Identifier}' no está declarada.",
                    "Declare la función o revise si el nombre está bien escrito.");
            }

            ResolveArguments(call);
            return TmType.Unknown;
        }

        function.IsInvoked = true;

        if (function.Parameters.Count != call.Arguments.Count)
        {
            ResolveArguments(call);

            _diagnostics.ReportError(
                DiagnosticCodes.WrongArgumentCount,
                call.Line,
                $"La función '{function.Name}' espera {function.Parameters.Count} argumento(s) " +
                $"y recibió {call.Arguments.Count}.",
                $"Ajuste la llamada a la firma declarada en la línea {function.Line}.");

            return function.ReturnType;
        }

        CheckArgumentTypes(call, function);

        if (!asStatement && function.ReturnType.Kind == TypeKind.Void)
        {
            _diagnostics.ReportError(
                DiagnosticCodes.IncompatibleOperation,
                call.Line,
                $"La función '{function.Name}' es de tipo 'void' y no produce un valor " +
                "utilizable en una expresión.",
                "Invóquela como sentencia o declare un tipo de retorno distinto de 'void'.");

            return TmType.Unknown;
        }

        return function.ReturnType;
    }

    private void CheckArgumentTypes(CallExpressionNode call, FunctionSymbol function)
    {
        for (int index = 0; index < call.Arguments.Count; index++)
        {
            VariableSymbol parameter = function.Parameters[index];
            TmType argument = Resolve(call.Arguments[index], parameter.Type);

            if (TypeCompatibility.IsAssignable(parameter.Type, argument))
            {
                continue;
            }

            _diagnostics.ReportError(
                DiagnosticCodes.IncompatibleArguments,
                call.Line,
                $"El argumento {index + 1} de '{function.Name}' es de tipo '{argument}' " +
                $"y se esperaba '{parameter.Type}'.",
                "Convierta el valor o ajuste el tipo del parámetro.");
        }
    }

    private void ResolveArguments(CallExpressionNode call)
    {
        foreach (ExpressionNode argument in call.Arguments)
        {
            Resolve(argument);
        }
    }

    /// <summary>
    /// Resuelve una indexación tratando la cadena completa (<c>m[i][j]</c>) de
    /// una vez: la aridad de índices depende de si la base es
    /// <c>sequence</c> (uno) o <c>score</c> (dos).
    /// </summary>
    private TmType ResolveIndex(IndexExpressionNode index)
    {
        var indices = new List<ExpressionNode>();
        ExpressionNode current = index;

        while (current is IndexExpressionNode indexed)
        {
            indices.Insert(0, indexed.Index);
            current = indexed.Target;
        }

        TmType baseType = Resolve(current);

        foreach (ExpressionNode indexExpression in indices)
        {
            TmType indexType = Resolve(indexExpression);

            if (!indexType.IsUnknown && indexType.Kind != TypeKind.Sample)
            {
                _diagnostics.ReportError(
                    DiagnosticCodes.IncompatibleOperation,
                    index.Line,
                    $"El índice debe ser de tipo 'sample' y es de tipo '{indexType}'.",
                    "Use una expresión entera como índice.");

                return TmType.Unknown;
            }
        }

        if (baseType.IsUnknown)
        {
            return TmType.Unknown;
        }

        int expectedIndices = baseType.Kind switch
        {
            TypeKind.Sequence => 1,
            TypeKind.Score => 2,
            _ => 0
        };

        if (expectedIndices == 0)
        {
            _diagnostics.ReportError(
                DiagnosticCodes.IncompatibleOperation,
                index.Line,
                $"No se puede indexar un valor de tipo '{baseType}'.",
                "La indexación solo se aplica a 'sequence' y 'score'.");

            return TmType.Unknown;
        }

        if (indices.Count != expectedIndices)
        {
            _diagnostics.ReportError(
                DiagnosticCodes.IncompatibleOperation,
                index.Line,
                $"Un valor de tipo '{baseType}' requiere {expectedIndices} índice(s) " +
                $"y se usaron {indices.Count}.",
                "Un 'sequence' se indexa con [i] y un 'score' con [fila][columna].");

            return TmType.Unknown;
        }

        return baseType.ElementType;
    }

    private TmType ResolveLength(LengthExpressionNode length)
    {
        TmType target = Resolve(length.Target);

        if (target.IsUnknown)
        {
            return TmType.Unknown;
        }

        if (!target.IsComposite)
        {
            _diagnostics.ReportError(
                DiagnosticCodes.IncompatibleOperation,
                length.Line,
                $"'.length()' no está definido para el tipo '{target}'.",
                "Solo 'sequence' y 'score' exponen 'length()'.");

            return TmType.Unknown;
        }

        return TmType.Sample;
    }

    private TmType ResolveCollectionLiteral(
        CollectionLiteralExpressionNode collection,
        TmType expectedType)
    {
        if (expectedType is not null && expectedType.Kind == TypeKind.Score)
        {
            return ResolveScoreLiteral(collection, expectedType);
        }

        if (expectedType is not null && expectedType.Kind == TypeKind.Sequence)
        {
            ResolveElements(collection, expectedType.ElementType);
            return expectedType;
        }

        return SynthesizeCollectionLiteral(collection);
    }

    private TmType ResolveScoreLiteral(
        CollectionLiteralExpressionNode collection,
        TmType expectedType)
    {
        int? columnCount = null;

        foreach (ExpressionNode element in collection.Elements)
        {
            if (element is not CollectionLiteralExpressionNode row)
            {
                TmType elementType = Resolve(element, expectedType.ElementType);

                _diagnostics.ReportError(
                    DiagnosticCodes.IncompatibleAssignment,
                    collection.Line,
                    $"Un valor de tipo '{expectedType}' se inicializa con filas, " +
                    $"y se encontró un elemento de tipo '{elementType}'.",
                    "Escriba la matriz como una lista de filas: [[1, 2], [3, 4]].");

                continue;
            }

            if (columnCount is null)
            {
                columnCount = row.Elements.Count;
            }
            else if (columnCount != row.Elements.Count)
            {
                _diagnostics.ReportError(
                    DiagnosticCodes.RaggedMatrix,
                    row.Line,
                    $"Todas las filas deben tener {columnCount} columna(s) y esta tiene " +
                    $"{row.Elements.Count}.",
                    "Iguale la cantidad de columnas de todas las filas.");
            }

            ResolveElements(row, expectedType.ElementType);
        }

        return expectedType;
    }

    private TmType SynthesizeCollectionLiteral(CollectionLiteralExpressionNode collection)
    {
        if (collection.Elements.Count == 0)
        {
            _diagnostics.ReportError(
                DiagnosticCodes.IncompatibleOperation,
                collection.Line,
                "No se puede inferir el tipo de una colección vacía.",
                "Declare el tipo de la variable para que la colección lo herede.");

            return TmType.Unknown;
        }

        if (collection.Elements[0] is CollectionLiteralExpressionNode firstRow)
        {
            TmType elementType = firstRow.Elements.Count > 0
                ? Resolve(firstRow.Elements[0])
                : TmType.Unknown;

            return ResolveScoreLiteral(collection, TmType.ScoreOf(elementType));
        }

        TmType inferred = Resolve(collection.Elements[0]);
        ResolveElements(collection, inferred);

        return TmType.SequenceOf(inferred);
    }

    private void ResolveElements(CollectionLiteralExpressionNode collection, TmType elementType)
    {
        foreach (ExpressionNode element in collection.Elements)
        {
            TmType actual = Resolve(element, elementType);

            if (TypeCompatibility.IsAssignable(elementType, actual))
            {
                continue;
            }

            _diagnostics.ReportError(
                DiagnosticCodes.IncompatibleAssignment,
                collection.Line,
                $"Los elementos deben ser de tipo '{elementType}' y se encontró uno de " +
                $"tipo '{actual}'.",
                "Las colecciones de TM son homogéneas: use un único tipo de elemento.");
        }
    }

    private void ReportUndeclared(string name, int line)
    {
        _diagnostics.ReportError(
            DiagnosticCodes.UndeclaredIdentifier,
            line,
            $"El identificador '{name}' no está declarado.",
            "Declare la variable antes de usarla o revise si el nombre está bien escrito.");

        // Se registra con tipo desconocido y marcado como usado para que los
        // usos posteriores del mismo nombre no repitan el error.
        _symbols.DeclareVariable(
            new VariableSymbol(name, TmType.Unknown, SymbolKind.Variable, line) { IsUsed = true });
    }

    private static string Describe(TokenType op)
    {
        switch (op)
        {
            case TokenType.Plus: return "+";
            case TokenType.Minus: return "-";
            case TokenType.Multiply: return "*";
            case TokenType.Divide: return "/";
            case TokenType.Equal: return "==";
            case TokenType.NotEqual: return "!=";
            case TokenType.GreaterThan: return ">";
            case TokenType.LessThan: return "<";
            case TokenType.GreaterOrEqual: return ">=";
            case TokenType.LessOrEqual: return "<=";
            case TokenType.And: return "and";
            case TokenType.Or: return "or";
            case TokenType.Not: return "not";
            case TokenType.Increment: return "++";
            case TokenType.Decrement: return "--";
            default: return op.ToString();
        }
    }
}
