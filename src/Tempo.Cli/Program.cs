using Tempo.Cli;
using Tempo.Language.Binding;
using Tempo.Language.Evaluation;
using Tempo.Language.IO;
using Tempo.Language.Syntax;


// Arnés de consola del pipeline completo de TM.
// Ejecuta análisis léxico, sintáctico y semántico.
// Si el archivo principal es .tmpr y no existen errores,
// ejecuta también el intérprete.

// La especificación (§7.2) reserva E001-E020 para errores semánticos y deja los
// errores léxicos y sintácticos como "E###" genérico (§10.2), sin número
// asignado: se emiten con E000 para no colisionar con el catálogo.
const string GenericErrorCode = "E000";

if (args.Length == 0)
{
    Console.WriteLine(
        "Uso: TmCompiler.Cli <archivo.tm|archivo.tmpr> [--tokens]");

    return;
}

string path = args[0];

bool showTokens =
    Array.Exists(
        args,
        argument =>
            string.Equals(
                argument,
                "--tokens",
                StringComparison.Ordinal));

if (!File.Exists(path))
{
    Console.Error.WriteLine(
        $"No se encontró el archivo '{path}'.");

    Environment.ExitCode = 1;

    return;
}

string source =
    File.ReadAllText(path);

try
{
    // Analisis lexico
    var lexer =
        new Lexer(
            source,
            path);

    List<Token> tokens =
        lexer.Tokenize().ToList();

    if (showTokens)
    {
        Console.WriteLine(
            LexerDiagnosticReporter.GenerateTokenReport(
                tokens,
                path));
    }

    // Analisis sintactico
    var parser =
        new Parser(
            tokens,
            path);

    ProgramNode program =
        parser.Parse();

    Console.WriteLine(
        AstDiagnosticReporter.GenerateAstReport(
            program,
            path));

    // Analisis semantico
    var semanticAnalyzer =
        new SemanticAnalyzer(
            program,
            path);

    SemanticAnalysisResult semanticResult =
        semanticAnalyzer.Analyze();

    Console.WriteLine(
        SemanticDiagnosticReporter.GenerateDiagnosticReport(
            semanticResult.Diagnostics,
            path));

    if (semanticResult.HasErrors)
    {
        Environment.ExitCode = 1;

        return;
    }

    // Los archivos .tm son modulos y no poseen un punto de entrada
    // ejecutable. El interprete unicamente se inicia para un .tmpr.
    bool isExecutableProgram =
        string.Equals(
            Path.GetExtension(path),
            ".tmpr",
            StringComparison.OrdinalIgnoreCase);

    if (!isExecutableProgram)
    {
        return;
    }

    // Ejecucion
    var runtimeInput =
        new ConsoleRuntimeInput();

    var runtimeOutput =
        new ConsoleRuntimeOutput();

    var interpreter =
        new Interpreter(
            program,
            runtimeInput,
            runtimeOutput);

    interpreter.Execute();
}
catch (LexerException ex)
{
    Console.Error.Write(
        ex.ToStandardFormat(
            GenericErrorCode,
            ex.Message,
            "Revise el texto señalado y corríjalo según el alfabeto léxico de TM."));

    Environment.ExitCode = 1;
}
catch (ParserException ex)
{
    Console.Error.Write(
        ex.ToStandardFormat(
            GenericErrorCode,
            ex.Message,
            "Revise la estructura de la sentencia según la gramática de TM."));

    Environment.ExitCode = 1;
}
catch (TmRuntimeException ex)
{
    Console.Error.WriteLine(
        $"{ex.Code} (línea {ex.Line}): {ex.Message}");

    Environment.ExitCode = 1;
}