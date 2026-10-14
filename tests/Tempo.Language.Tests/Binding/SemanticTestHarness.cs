using Tempo.Language.Binding;
using Tempo.Language.Diagnostics;
using Tempo.Language.Syntax;

namespace Tempo.Language.Tests.Binding;

/// <summary>
/// Arranque compartido de las pruebas semánticas: replica el encadenamiento
/// real del compilador (Lexer, Parser y analizador) sobre código fuente TM.
/// </summary>

public static class SemanticTestHarness
{
    public const string DefaultFileName = "test.tmpr";

    public static IReadOnlyList<Diagnostic> Analyze(
        string source,
        string fileName = DefaultFileName)
    {
        return new SemanticAnalyzer(Parse(source, fileName), fileName).Analyze().Diagnostics;
    }

    public static ProgramNode Parse(string source, string fileName = DefaultFileName)
    {
        var lexer = new Lexer(source, fileName);
        List<Token> tokens = lexer.Tokenize().ToList();

        var parser = new Parser(tokens, fileName);
        return parser.Parse();
    }

    /// <summary>Devuelve el cuerpo de la primera función del programa indicado.</summary>
    public static BlockNode FirstFunctionBody(string source)
    {
        return Parse(source).Functions[0].Body;
    }

    /// <summary>Envuelve el cuerpo indicado en una sentencia session.</summary>
    public static IReadOnlyList<Diagnostic> AnalyzeSession(string body)
    {
        return Analyze($"session >\n{body}\n<");
    }

    public static IReadOnlyList<Diagnostic> ErrorsOf(IEnumerable<Diagnostic> diagnostics)
    {
        return diagnostics.Where(diagnostic => diagnostic.IsError).ToList();
    }

    public static bool HasCode(IEnumerable<Diagnostic> diagnostics, string code)
    {
        return diagnostics.Any(diagnostic => diagnostic.Code == code);
    }

    /// <summary>Devuelve el único diagnóstico con ese código; falla si no hay exactamente uno.</summary>
    public static Diagnostic Single(IEnumerable<Diagnostic> diagnostics, string code)
    {
        return diagnostics.Single(diagnostic => diagnostic.Code == code);
    }

    /// <summary>Códigos de todos los diagnósticos, para afirmar sobre el conjunto completo.</summary>
    public static string[] CodesOf(IEnumerable<Diagnostic> diagnostics)
    {
        return diagnostics.Select(diagnostic => diagnostic.Code).ToArray();
    }
}
