using Tempo.Language.Diagnostics;

namespace Tempo.Language.Syntax;

/// <summary>
/// Un archivo fuente analizado: su AST junto con los errores léxicos o
/// sintácticos que impidieron construirlo. Es la unidad con la que trabaja
/// <see cref="Compilation"/>; como los errores se guardan como diagnósticos en
/// lugar de propagarse, un archivo con errores no detiene el análisis de los
/// demás archivos del proyecto.
/// </summary>

public sealed class SyntaxTree
{
    public const string ProjectFileExtension = ".tmpr";

    private SyntaxTree(
        string fullPath,
        string fileName,
        ProgramNode root,
        IReadOnlyList<Diagnostic> diagnostics)
    {
        FullPath = fullPath;
        FileName = fileName;
        Root = root;
        Diagnostics = diagnostics;
    }

    public string FullPath { get; }

    /// <summary>Nombre con el que el archivo aparece en los diagnósticos.</summary>
    public string FileName { get; }

    /// <summary>AST del archivo, o <c>null</c> si el análisis léxico o sintáctico falló.</summary>
    public ProgramNode Root { get; }

    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    public bool IsParsed => Root is not null;

    public bool IsProjectFile => FullPath.EndsWith(ProjectFileExtension, StringComparison.OrdinalIgnoreCase);

    public bool IsModule => !IsProjectFile;

    public string DirectoryPath => Path.GetDirectoryName(FullPath);

    public static SyntaxTree Load(string fullPath, string fileName)
    {
        return Parse(File.ReadAllText(fullPath), fullPath, fileName);
    }

    public static SyntaxTree Parse(string text, string fullPath, string fileName)
    {
        try
        {
            List<Token> tokens = new Lexer(text, fileName).Tokenize().ToList();
            ProgramNode root = new Parser(tokens, fileName).Parse();

            return new SyntaxTree(fullPath, fileName, root, Array.Empty<Diagnostic>());
        }
        catch (LexerException ex)
        {
            return Failed(
                fullPath,
                fileName,
                "Error léxico",
                ex.Line,
                ex.Message,
                "Revise el texto señalado y corríjalo según el alfabeto léxico de TM.");
        }
        catch (ParserException ex)
        {
            return Failed(
                fullPath,
                fileName,
                "Error sintáctico",
                ex.Line,
                ex.Message,
                "Revise la estructura de la sentencia según la gramática de TM.");
        }
    }

    private static SyntaxTree Failed(
        string fullPath,
        string fileName,
        string title,
        int line,
        string description,
        string suggestion)
    {
        var diagnostic = new Diagnostic(
            DiagnosticSeverity.Error,
            DiagnosticCodes.LexicalOrSyntaxError,
            title,
            fileName,
            line,
            description,
            suggestion);

        return new SyntaxTree(fullPath, fileName, null, new[] { diagnostic });
    }
}
