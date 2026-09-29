using TmCompiler.Application.Analysis;
using TmCompiler.Application.Lexing;
using TmCompiler.Application.Parsing;
using TmCompiler.Domain.Lexical;
using TmCompiler.Domain.Syntactic;
using TmCompiler.Infrastructure;


// Arnés de consola del pipeline del compilador, para probarlo de forma aislada
// sin depender de TmIde. Ejecuta las fases léxica, sintáctica y semántica.

// La especificación (§7.2) reserva E001-E020 para errores semánticos y deja los
// errores léxicos y sintácticos como "E###" genérico (§10.2), sin número
// asignado: se emiten con E000 para no colisionar con el catálogo.
const string GenericErrorCode = "E000";

if (args.Length == 0)
{
    Console.WriteLine("Uso: TmCompiler.Cli <archivo.tm|archivo.tmpr> [--tokens]");
    return;
}

string path = args[0];
bool showTokens = Array.Exists(args, argument => argument == "--tokens");

if (!File.Exists(path))
{
    Console.Error.WriteLine($"No se encontró el archivo '{path}'.");
    Environment.ExitCode = 1;
    return;
}

string source = File.ReadAllText(path);

try
{
    //Analisis lexico
    ILexer lexer = new Lexer(source, path);
    List<Token> tokens = lexer.Tokenize().ToList();

    if (showTokens)
    {
        Console.WriteLine(LexerDiagnosticReporter.GenerateTokenReport(tokens, path));
    }

    //analisis sintactico
    IParser parser = new Parser(tokens, path);
    ProgramNode program = parser.Parse();

    Console.WriteLine(AstDiagnosticReporter.GenerateAstReport(program, path));

    //analisis semantico
    ISemanticAnalyzer semanticAnalyzer = new SemanticAnalyzer(program, path);
    SemanticAnalysisResult semanticResult = semanticAnalyzer.Analyze();

    Console.WriteLine(
        SemanticDiagnosticReporter.GenerateDiagnosticReport(
            semanticResult.Diagnostics,
            path));

    if (semanticResult.HasErrors)
    {
        Environment.ExitCode = 1;
        return;
    }
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
