using Tempo.Cli;
using Tempo.Language;
using Tempo.Language.Diagnostics;
using Tempo.Language.Evaluation;
using Tempo.Language.IO;
using Tempo.Language.Syntax;


// Arnés de consola del pipeline completo de TM.
// Compila un proyecto y, si no hay errores y la entrada es un .tmpr, lo ejecuta.
//  - Con un directorio, compila todos sus archivos como un proyecto.
//  - Con un archivo, compila ese archivo y los que importa.

if (args.Length == 0)
{
    Console.WriteLine(
        "Uso: Tempo.Cli <directorio|archivo.tmpr|archivo.tm> [--tokens] [--ast]");

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

bool showAst =
    Array.Exists(
        args,
        argument =>
            string.Equals(
                argument,
                "--ast",
                StringComparison.Ordinal));

if (!File.Exists(path) && !Directory.Exists(path))
{
    Console.Error.WriteLine(
        $"No se encontró el archivo o directorio '{path}'.");

    Environment.ExitCode = 1;

    return;
}

Compilation compilation =
    Compilation.FromPath(path);

SyntaxTree entry =
    compilation.Project.EntryFile;

if (entry is { IsParsed: true })
{
    if (showTokens)
    {
        List<Token> tokens =
            new Lexer(
                File.ReadAllText(entry.FullPath),
                entry.FileName)
            .Tokenize()
            .ToList();

        Console.WriteLine(
            LexerDiagnosticReporter.GenerateTokenReport(
                tokens,
                entry.FileName));
    }

    if (showAst)
    {
        Console.WriteLine(
            AstDiagnosticReporter.GenerateAstReport(
                entry.Root,
                entry.FileName));
    }
}

// Un reporte por archivo: siempre el de entrada, y los demás solo si tienen
// diagnósticos.
foreach (SyntaxTree file in compilation.Project.Files)
{
    List<Diagnostic> diagnostics =
        compilation.DiagnosticsOf(file).ToList();

    if (file != entry && diagnostics.Count == 0)
    {
        continue;
    }

    Console.WriteLine(
        SemanticDiagnosticReporter.GenerateDiagnosticReport(
            diagnostics,
            file.FileName));
}

// Diagnósticos que no pertenecen a ningún archivo (por ejemplo, E016).
List<Diagnostic> projectDiagnostics =
    compilation.Diagnostics
        .Where(diagnostic =>
            !compilation.Project.Files.Any(file => file.FileName == diagnostic.FileName))
        .ToList();

if (projectDiagnostics.Count > 0)
{
    Console.WriteLine(
        SemanticDiagnosticReporter.GenerateDiagnosticReport(
            projectDiagnostics,
            compilation.Project.RootDirectory));
}

if (compilation.HasErrors)
{
    Environment.ExitCode = 1;

    return;
}

// Los archivos .tm son módulos y no poseen un punto de entrada ejecutable.
if (!compilation.IsExecutable)
{
    return;
}

try
{
    compilation.Run(
        new ConsoleRuntimeInput(),
        new ConsoleRuntimeOutput());
}
catch (TmRuntimeException ex)
{
    string suggestion = ex.Code switch
    {
        RuntimeErrorCodes.DivisionByZero =>
            "Verifique que el divisor sea diferente de cero.",

        RuntimeErrorCodes.IndexOutOfBounds =>
            "Verifique que el indice este dentro de los limites de la coleccion.",

        RuntimeErrorCodes.ArithmeticOverflow =>
            "Utilice valores que no excedan el rango permitido por el tipo de dato.",

        RuntimeErrorCodes.InputParseError =>
            "Ingrese un valor compatible con el tipo de dato esperado.",

        RuntimeErrorCodes.UndefinedFunction =>
            "Verifique que la funcion exista y este correctamente declarada.",

        RuntimeErrorCodes.InputEndOfFile =>
            "Proporcione los datos de entrada necesarios antes de finalizar la entrada.",

        RuntimeErrorCodes.InvalidToneInput =>
            "Ingrese un caracter valido para el tipo tone.",

        _ => "Revise la operacion que produjo el error."
    };

    string fileName =
        ex.FileName ?? compilation.Project.EntryFile.FileName;

    var diagnostic = new Diagnostic(
        DiagnosticSeverity.Error,
        ex.Code,
        DiagnosticCatalog.TitleOf(ex.Code),
        fileName,
        ex.Line,
        ex.Message,
        suggestion);

    Console.Error.WriteLine(
        diagnostic.ToStandardFormat());

    Environment.ExitCode = 2;
}
