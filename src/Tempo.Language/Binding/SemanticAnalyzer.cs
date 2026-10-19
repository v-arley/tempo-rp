using Tempo.Language.Diagnostics;
using Tempo.Language.Symbols;
using Tempo.Language.Syntax;

namespace Tempo.Language.Binding;

/// <summary>
/// Orquesta el análisis semántico de un archivo: recolecta firmas, analiza los
/// cuerpos y cierra con las comprobaciones globales. A diferencia del lexer y
/// el parser, no lanza ante un programa incorrecto: acumula los diagnósticos y
/// continúa, de modo que una sola ejecución reporta todos los problemas.
/// </summary>

public sealed class SemanticAnalyzer : ISemanticAnalyzer
{
    private const string ProjectFileExtension = ".tmpr";

    private readonly ProgramNode _program;
    private readonly string _fileName;
    private readonly IReadOnlyCollection<FunctionSymbol> _importedFunctions;

    public SemanticAnalyzer(ProgramNode program, string fileName)
        : this(program, fileName, null)
    {
    }

    public SemanticAnalyzer(
        ProgramNode program,
        string fileName,
        IReadOnlyCollection<FunctionSymbol> importedFunctions)
    {
        ArgumentNullException.ThrowIfNull(program);

        _program = program;
        _fileName = fileName ?? "<unknown>";
        _importedFunctions = importedFunctions;
    }

    /// <summary>
    /// Cuando es <c>false</c>, W003 queda a cargo del análisis de proyecto, que
    /// conoce las llamadas hechas desde todos los archivos.
    /// </summary>
    public bool ReportsUninvokedFunctions { get; init; } = true;

    /// <summary>
    /// Funciones declaradas en archivos del proyecto que este archivo no importa,
    /// con el archivo que declara cada una. Llamarlas produce E020 en lugar de E001.
    /// </summary>
    public IReadOnlyDictionary<string, string> UnimportedFunctions { get; init; }

    public SemanticAnalysisResult Analyze()
    {
        var symbols = new SymbolTable();
        var diagnostics = new DiagnosticBag(_fileName);
        var resolver = new ExpressionTypeResolver(symbols, diagnostics)
        {
            // Sin las firmas de los archivos importados no se puede afirmar que
            // un nombre desconocido no exista: se anota para el análisis de proyecto.
            HasUnresolvedImports = _program.Imports.Count > 0 && _importedFunctions is null,
            UnimportedFunctions = UnimportedFunctions
        };

        var statements = new StatementAnalyzer(symbols, diagnostics, resolver);
        var collector = new FunctionSignatureCollector(symbols, diagnostics);

        collector.Seed(_importedFunctions);
        collector.Collect(_program);

        foreach (FunctionDeclarationNode function in _program.Functions)
        {
            AnalyzeFunction(function, symbols, diagnostics, statements);
        }

        AnalyzeSession(symbols, diagnostics, statements);

        if (ReportsUninvokedFunctions)
        {
            ReportUninvokedFunctions(symbols, diagnostics);
        }

        return new SemanticAnalysisResult(
            diagnostics.Diagnostics,
            symbols.Functions.ToList(),
            resolver.UnresolvedFunctionCalls.ToList());
    }

    private void AnalyzeFunction(
        FunctionDeclarationNode function,
        SymbolTable symbols,
        DiagnosticBag diagnostics,
        StatementAnalyzer statements)
    {
        FunctionSymbol symbol = symbols.ResolveFunction(function.Identifier);
        TmType returnType = symbol?.ReturnType ?? TmType.Unknown;

        // Los parámetros viven en el mismo ámbito que el cuerpo (§5.1.3).
        symbols.EnterScope();

        foreach (ParameterNode parameter in function.Parameters)
        {
            DeclareParameter(parameter, symbols, diagnostics);
        }

        statements.CurrentReturnType = returnType;
        statements.CurrentFunctionName = function.Identifier;
        statements.AnalyzeStatements(function.Body);

        if (returnType.Kind != TypeKind.Void &&
            !returnType.IsUnknown &&
            !ReturnPathAnalyzer.AlwaysReturns(function.Body))
        {
            diagnostics.ReportError(
                DiagnosticCodes.InvalidReturn,
                function.Line,
                $"La función '{function.Identifier}' declara el tipo de retorno " +
                $"'{returnType}' pero no devuelve un valor en todas las trayectorias.",
                "Añada un 'ret' final o una rama 'else' que también retorne.");
        }

        statements.CloseScope();
        statements.CurrentReturnType = null;
        statements.CurrentFunctionName = null;
    }

    private void AnalyzeSession(
        SymbolTable symbols,
        DiagnosticBag diagnostics,
        StatementAnalyzer statements)
    {

        // Detectar declaraciones session duplicadas en el archivo principal
        if (_fileName.EndsWith(
            ProjectFileExtension,
            StringComparison.OrdinalIgnoreCase))
        {
            foreach (SessionNode duplicateSession in _program.AdditionalSessions)
            {
                diagnostics.ReportError(
                    DiagnosticCodes.SessionMissingOrDuplicated,
                    duplicateSession.Line,
                    "El archivo de proyecto declara más de una sentencia 'session'.",
                    "Conserve una sola sentencia 'session' en el archivo .tmpr.");
            }
        }

        if (_program.Session is null)
        {
            // Solo el archivo de proyecto está obligado a tener punto de entrada;
            // un módulo .tm sin session es válido.
            if (_fileName.EndsWith(ProjectFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                diagnostics.ReportError(
                    DiagnosticCodes.SessionMissingOrDuplicated,
                    1,
                    "El archivo de proyecto no declara ninguna sentencia 'session'.",
                    "Añada el punto de entrada: session > ... <");
            }

            return;
        }

        symbols.EnterScope();

        statements.CurrentReturnType = TmType.Void;
        statements.CurrentFunctionName = "session";
        statements.AnalyzeStatements(_program.Session.Body);

        statements.CloseScope();
        statements.CurrentReturnType = null;
        statements.CurrentFunctionName = null;
    }

    private static void DeclareParameter(
        ParameterNode parameter,
        SymbolTable symbols,
        DiagnosticBag diagnostics)
    {
        TmType parameterType = TypeNameParser.ParseOrUnknown(parameter.TypeName);

        if (parameterType.Kind == TypeKind.Void)
        {
            diagnostics.ReportError(
                DiagnosticCodes.IncompatibleOperation,
                parameter.Line,
                $"El parámetro '{parameter.Identifier}' no puede ser de tipo 'void'.",
                "Use un tipo primitivo o compuesto.");

            parameterType = TmType.Unknown;
        }
        else if (parameterType.Kind == TypeKind.Silence)
        {
            diagnostics.ReportError(
                DiagnosticCodes.InvalidSilenceUsage,
                parameter.Line,
                $"El parámetro '{parameter.Identifier}' no puede ser de tipo 'silence'.",
                "Declare el parámetro como 'phrase' si debe admitir 'silence'.");

            parameterType = TmType.Unknown;
        }

        DeclarationResult result = symbols.DeclareVariable(
            new VariableSymbol(
                parameter.Identifier,
                parameterType,
                SymbolKind.Parameter,
                parameter.Line));

        if (result.Status == DeclarationStatus.Duplicated)
        {
            diagnostics.ReportError(
                DiagnosticCodes.Redeclaration,
                parameter.Line,
                $"El parámetro '{parameter.Identifier}' está repetido en la firma.",
                "Cada parámetro debe tener un nombre distinto.");
        }
    }

    private static void ReportUninvokedFunctions(SymbolTable symbols, DiagnosticBag diagnostics)
    {
        foreach (FunctionSymbol function in symbols.Functions.OrderBy(function => function.Line))
        {
            if (function.IsInvoked)
            {
                continue;
            }

            diagnostics.ReportWarning(
                DiagnosticCodes.UnusedFunction,
                function.Line,
                $"La función '{function.Name}' se declara pero nunca se invoca.",
                "Elimínela o invóquela desde 'session' u otra función.");
        }
    }
}
