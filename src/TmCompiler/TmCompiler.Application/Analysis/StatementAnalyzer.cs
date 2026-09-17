using TmCompiler.Domain.Diagnostics;
using TmCompiler.Domain.Lexical;
using TmCompiler.Domain.Semantic;
using TmCompiler.Domain.Syntactic;

namespace TmCompiler.Application.Analysis;

/// <summary>
/// Recorre las sentencias de un cuerpo aplicando las reglas de la semántica
/// estática (§5). El AST no expone un visitante, así que el despacho se hace
/// con coincidencia de patrones sobre el tipo concreto de cada nodo.
/// </summary>

public sealed class StatementAnalyzer
{
    private readonly SymbolTable _symbols;
    private readonly DiagnosticBag _diagnostics;
    private readonly ExpressionTypeResolver _resolver;

    public StatementAnalyzer(
        SymbolTable symbols,
        DiagnosticBag diagnostics,
        ExpressionTypeResolver resolver)
    {
        _symbols = symbols;
        _diagnostics = diagnostics;
        _resolver = resolver;
    }

    /// <summary>Tipo de retorno del cuerpo en curso; <c>void</c> dentro de session.</summary>
    public TmType CurrentReturnType { get; set; }

    public string CurrentFunctionName { get; set; }

    /// <summary>
    /// Analiza las sentencias sin abrir un ámbito nuevo. Lo usan el cuerpo de
    /// una función (que comparte ámbito con sus parámetros, §5.1.3) y el cuerpo
    /// de un <c>loop</c> (que lo comparte con su inicializador, §5.1.4).
    /// </summary>
    public void AnalyzeStatements(BlockNode block)
    {
        if (block is null)
        {
            return;
        }

        foreach (StatementNode statement in block.Statements)
        {
            Analyze(statement);
        }
    }

    /// <summary>Analiza el bloque en un ámbito propio y advierte de lo no usado al cerrarlo.</summary>
    public void AnalyzeBlock(BlockNode block)
    {
        if (block is null)
        {
            return;
        }

        _symbols.EnterScope();
        AnalyzeStatements(block);
        CloseScope();
    }

    /// <summary>Cierra el ámbito actual emitiendo la advertencia de variables sin usar.</summary>
    public void CloseScope()
    {
        IReadOnlyCollection<VariableSymbol> symbols = _symbols.ExitScope();

        foreach (VariableSymbol symbol in symbols.OrderBy(symbol => symbol.Line))
        {
            // Los parámetros forman parte de la firma: no usarlos no es un descuido.
            if (symbol.IsUsed || symbol.Kind == SymbolKind.Parameter)
            {
                continue;
            }

            _diagnostics.ReportWarning(
                DiagnosticCodes.UnusedVariable,
                symbol.Line,
                $"El identificador '{symbol.Name}' se declara pero nunca se utiliza.",
                "Elimine la declaración o utilice el valor.");
        }
    }

    private void Analyze(StatementNode statement)
    {
        switch (statement)
        {
            case VariableDeclarationNode declaration:
                AnalyzeDeclaration(
                    declaration.TypeName,
                    declaration.Identifier,
                    declaration.Initializer,
                    SymbolKind.Variable,
                    declaration.Line);
                break;

            case ConstantDeclarationNode constant:
                AnalyzeConstantDeclaration(constant);
                break;

            case AssignmentStatementNode assignment:
                AnalyzeAssignment(assignment);
                break;

            case IndexedAssignmentStatementNode indexed:
                AnalyzeIndexedAssignment(indexed);
                break;

            case UpdateStatementNode update:
                AnalyzeUpdate(update.Identifier, update.Operator, update.Line);
                break;

            case InputStatementNode input:
                AnalyzeInput(input);
                break;

            case OutputStatementNode output:
                AnalyzeOutput(output);
                break;

            case ReturnStatementNode returnStatement:
                AnalyzeReturn(returnStatement);
                break;

            case ExpressionStatementNode expressionStatement:
                AnalyzeExpressionStatement(expressionStatement);
                break;

            case IfStatementNode ifStatement:
                AnalyzeIf(ifStatement);
                break;

            case LoopStatementNode loop:
                AnalyzeLoop(loop);
                break;

            case RepeatStatementNode repeat:
                AnalyzeCondition(repeat.Condition, repeat.Line);
                AnalyzeBlock(repeat.Body);
                break;

            case MixStatementNode mix:
                AnalyzeMix(mix);
                break;

            default:
                break;
        }
    }

    private void AnalyzeDeclaration(
        string typeName,
        string identifier,
        ExpressionNode initializer,
        SymbolKind kind,
        int line)
    {
        TmType declaredType = TypeNameParser.ParseOrUnknown(typeName);

        if (!IsDeclarableType(declaredType, identifier, line))
        {
            declaredType = TmType.Unknown;
        }

        TmType initializerType = _resolver.Resolve(initializer, declaredType);

        CheckAssignable(declaredType, initializerType, line, $"la variable '{identifier}'");

        Declare(new VariableSymbol(identifier, declaredType, kind, line));
    }

    private void AnalyzeConstantDeclaration(ConstantDeclarationNode constant)
    {
        if (!ConstantExpressionEvaluator.IsConstant(constant.Initializer))
        {
            _diagnostics.ReportError(
                DiagnosticCodes.NonConstantConstantInitializer,
                constant.Line,
                $"La constante '{constant.Identifier}' debe inicializarse con una expresión " +
                "evaluable en tiempo de compilación.",
                "Use literales u operaciones entre literales.");
        }

        AnalyzeDeclaration(
            constant.TypeName,
            constant.Identifier,
            constant.Initializer,
            SymbolKind.Constant,
            constant.Line);
    }

    private void AnalyzeAssignment(AssignmentStatementNode assignment)
    {
        VariableSymbol symbol = ResolveTarget(assignment.Identifier, assignment.Line);
        TmType targetType = symbol?.Type ?? TmType.Unknown;
        TmType valueType = _resolver.Resolve(assignment.Value, targetType);

        CheckAssignable(
            targetType,
            valueType,
            assignment.Line,
            $"la variable '{assignment.Identifier}'");
    }

    private void AnalyzeIndexedAssignment(IndexedAssignmentStatementNode indexed)
    {
        TmType targetType = _resolver.Resolve(indexed.Target);

        // La mutabilidad se decide sobre la variable en la base de la cadena de
        // índices: m[i][j] = x escribe en m.
        ExpressionNode current = indexed.Target;

        while (current is IndexExpressionNode index)
        {
            current = index.Target;
        }

        if (current is IdentifierExpressionNode identifier)
        {
            VariableSymbol symbol = _symbols.ResolveVariable(identifier.Name);
            CheckWritable(symbol, indexed.Line);
        }

        TmType valueType = _resolver.Resolve(indexed.Value, targetType);

        CheckAssignable(targetType, valueType, indexed.Line, "el elemento indexado");
    }

    private void AnalyzeUpdate(string identifier, TokenType op, int line)
    {
        VariableSymbol symbol = ResolveTarget(identifier, line);

        if (symbol is null || symbol.Type.IsUnknown)
        {
            return;
        }

        if (symbol.Type.Kind != TypeKind.Sample)
        {
            _diagnostics.ReportError(
                DiagnosticCodes.IncompatibleOperation,
                line,
                $"El operador '{(op == TokenType.Decrement ? "--" : "++")}' requiere un " +
                $"'sample' y '{identifier}' es de tipo '{symbol.Type}'.",
                "Aplique el incremento o decremento solo sobre variables enteras.");
        }
    }

    private void AnalyzeInput(InputStatementNode input)
    {
        VariableSymbol symbol = ResolveTarget(input.Identifier, input.Line);

        if (symbol is null || symbol.Type.IsUnknown)
        {
            return;
        }

        if (!symbol.Type.IsPrimitive)
        {
            _diagnostics.ReportError(
                DiagnosticCodes.IncompatibleOperation,
                input.Line,
                $"No se puede leer un valor de tipo '{symbol.Type}' desde la entrada estándar.",
                "Lea los elementos uno a uno en variables de tipo primitivo.");
        }
    }

    private void AnalyzeOutput(OutputStatementNode output)
    {
        TmType type = _resolver.Resolve(output.Expression);

        if (type.IsUnknown)
        {
            return;
        }

        if (!type.IsPrimitive && type.Kind != TypeKind.Silence)
        {
            _diagnostics.ReportError(
                DiagnosticCodes.IncompatibleOperation,
                output.Line,
                $"No se puede escribir un valor de tipo '{type}' en la salida estándar.",
                "Escriba los elementos uno a uno recorriendo la colección.");
        }
    }

    private void AnalyzeReturn(ReturnStatementNode returnStatement)
    {
        TmType expected = CurrentReturnType ?? TmType.Void;
        string owner = CurrentFunctionName ?? "session";

        if (returnStatement.Expression is null)
        {
            if (expected.Kind != TypeKind.Void)
            {
                _diagnostics.ReportError(
                    DiagnosticCodes.InvalidReturn,
                    returnStatement.Line,
                    $"'{owner}' declara el tipo de retorno '{expected}' y 'ret' no devuelve " +
                    "ningún valor.",
                    $"Devuelva un valor de tipo '{expected}'.");
            }

            return;
        }

        TmType actual = _resolver.Resolve(returnStatement.Expression, expected);

        if (expected.Kind == TypeKind.Void)
        {
            _diagnostics.ReportError(
                DiagnosticCodes.InvalidReturn,
                returnStatement.Line,
                $"'{owner}' es de tipo 'void' y no puede devolver un valor.",
                "Use 'ret;' sin expresión.");

            return;
        }

        CheckAssignable(expected, actual, returnStatement.Line, $"el retorno de '{owner}'");
    }

    private void AnalyzeExpressionStatement(ExpressionStatementNode expressionStatement)
    {
        if (expressionStatement.Expression is CallExpressionNode call)
        {
            _resolver.ResolveCallStatement(call);
            return;
        }

        _resolver.Resolve(expressionStatement.Expression);
    }

    private void AnalyzeIf(IfStatementNode ifStatement)
    {
        AnalyzeCondition(ifStatement.Condition, ifStatement.Line);
        AnalyzeBlock(ifStatement.ThenBlock);

        foreach (ElseIfClauseNode clause in ifStatement.ElseIfClauses)
        {
            AnalyzeCondition(clause.Condition, ifStatement.Line);
            AnalyzeBlock(clause.Body);
        }

        AnalyzeBlock(ifStatement.ElseBlock);
    }

    private void AnalyzeLoop(LoopStatementNode loop)
    {
        // Inicializador, condición, actualización y cuerpo comparten un único
        // ámbito (§5.1.4).
        _symbols.EnterScope();

        if (loop.Initializer is not null)
        {
            Analyze(loop.Initializer);
        }

        AnalyzeCondition(loop.Condition, loop.Line);
        AnalyzeLoopUpdate(loop.Update);
        AnalyzeStatements(loop.Body);

        CloseScope();
    }

    private void AnalyzeLoopUpdate(LoopUpdateNode update)
    {
        if (update is null)
        {
            return;
        }

        if (update.Operator is TokenType.Increment or TokenType.Decrement)
        {
            AnalyzeUpdate(update.Identifier, update.Operator, update.Line);
            return;
        }

        VariableSymbol symbol = ResolveTarget(update.Identifier, update.Line);
        TmType targetType = symbol?.Type ?? TmType.Unknown;
        TmType valueType = _resolver.Resolve(update.Value, targetType);

        CheckAssignable(targetType, valueType, update.Line, $"la variable '{update.Identifier}'");
    }

    private void AnalyzeMix(MixStatementNode mix)
    {
        TmType selector = _resolver.Resolve(mix.Expression);

        if (!selector.IsUnknown && !selector.IsPrimitive)
        {
            _diagnostics.ReportError(
                DiagnosticCodes.IncompatibleOperation,
                mix.Line,
                $"La expresión de control de 'mix' debe ser de un tipo primitivo y es " +
                $"'{selector}'.",
                "Use un 'sample', 'wave', 'tone', 'phrase' o 'signal' como selector.");

            selector = TmType.Unknown;
        }

        var seenCaseValues = new HashSet<string>(StringComparer.Ordinal);

        foreach (MixCaseNode mixCase in mix.Cases)
        {
            TmType caseType = _resolver.Resolve(mixCase.Value, selector);

            if (!TypeCompatibility.IsAssignable(selector, caseType))
            {
                _diagnostics.ReportError(
                    DiagnosticCodes.IncompatibleOperation,
                    mix.Line,
                    $"El valor del 'case' es de tipo '{caseType}' y el selector de " +
                    $"'{selector}'.",
                    "Todos los 'case' deben compartir el tipo de la expresión de control.");
            }

            // Solo se comparan literales: dos expresiones no constantes podrían
            // valer lo mismo sin que se pueda saber aquí.
            if (mixCase.Value is LiteralExpressionNode literal &&
                !seenCaseValues.Add(literal.Value))
            {
                _diagnostics.ReportError(
                    DiagnosticCodes.IncompatibleOperation,
                    mix.Line,
                    $"El valor '{literal.Value}' aparece en más de un 'case'.",
                    "Elimine el caso duplicado: solo se ejecutaría el primero.");
            }

            AnalyzeBlock(mixCase.Body);
        }

        AnalyzeBlock(mix.DefaultBlock);
    }

    private void AnalyzeCondition(ExpressionNode condition, int line)
    {
        TmType type = _resolver.Resolve(condition);

        if (type.IsUnknown || type.Kind == TypeKind.Signal)
        {
            return;
        }

        _diagnostics.ReportError(
            DiagnosticCodes.NonBooleanCondition,
            line,
            $"La condición debe producir un valor de tipo 'signal' y produce '{type}'.",
            "Use una comparación o un valor booleano como condición.");
    }

    private bool IsDeclarableType(TmType type, string identifier, int line)
    {
        if (type.Kind == TypeKind.Void)
        {
            _diagnostics.ReportError(
                DiagnosticCodes.IncompatibleOperation,
                line,
                $"'void' no es un tipo válido para '{identifier}': solo puede ser tipo de " +
                "retorno de una función.",
                "Declare la variable con un tipo primitivo o compuesto.");

            return false;
        }

        if (type.Kind == TypeKind.Silence)
        {
            _diagnostics.ReportError(
                DiagnosticCodes.InvalidSilenceUsage,
                line,
                $"'silence' no es un tipo declarable para '{identifier}': es el valor nulo.",
                "Declare la variable como 'phrase' si debe poder valer 'silence'.");

            return false;
        }

        return true;
    }

    private void CheckAssignable(TmType target, TmType source, int line, string description)
    {
        if (TypeCompatibility.IsSilenceMisuse(target, source))
        {
            _diagnostics.ReportError(
                DiagnosticCodes.InvalidSilenceUsage,
                line,
                $"No se puede asignar 'silence' a {description}, de tipo '{target}'.",
                "'silence' solo puede asignarse a un 'phrase'.");

            return;
        }

        if (TypeCompatibility.IsAssignable(target, source))
        {
            return;
        }

        _diagnostics.ReportError(
            DiagnosticCodes.IncompatibleAssignment,
            line,
            $"No se puede asignar un valor de tipo '{source}' a {description}, de tipo " +
            $"'{target}'.",
            "Use una función de conversión o cambie el tipo declarado.");
    }

    private VariableSymbol ResolveTarget(string identifier, int line)
    {
        VariableSymbol symbol = _symbols.ResolveVariable(identifier);

        if (symbol is null)
        {
            _diagnostics.ReportError(
                DiagnosticCodes.UndeclaredIdentifier,
                line,
                $"El identificador '{identifier}' no está declarado.",
                "Declare la variable antes de asignarle un valor.");

            _symbols.DeclareVariable(
                new VariableSymbol(identifier, TmType.Unknown, SymbolKind.Variable, line)
                {
                    IsUsed = true
                });

            return null;
        }

        CheckWritable(symbol, line);
        return symbol;
    }

    private void CheckWritable(VariableSymbol symbol, int line)
    {
        if (symbol is null || !symbol.IsReadOnly)
        {
            return;
        }

        _diagnostics.ReportError(
            DiagnosticCodes.ConstantWrite,
            line,
            $"No se puede modificar '{symbol.Name}': está declarada como 'const'.",
            "Use una variable 'var' si el valor debe cambiar.");
    }

    private void Declare(VariableSymbol symbol)
    {
        DeclarationResult result = _symbols.DeclareVariable(symbol);

        switch (result.Status)
        {
            case DeclarationStatus.Duplicated:
                _diagnostics.ReportError(
                    DiagnosticCodes.Redeclaration,
                    symbol.Line,
                    $"El identificador '{symbol.Name}' ya se declaró en este ámbito, en la " +
                    $"línea {result.Existing.Line}.",
                    "Use otro nombre o elimine la declaración repetida.");
                break;

            case DeclarationStatus.Shadowing:
                _diagnostics.ReportWarning(
                    DiagnosticCodes.ShadowedIdentifier,
                    symbol.Line,
                    $"'{symbol.Name}' oculta el identificador homónimo declarado en la línea " +
                    $"{result.Existing.Line}.",
                    "Renombre una de las dos declaraciones si el solapamiento no es intencionado.");
                break;

            default:
                break;
        }
    }
}
