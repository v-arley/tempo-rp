using System;
using System.Collections.Generic;
using System.Linq;

using TmCompiler.Application.Analysis;
using TmCompiler.Application.Lexing;
using TmCompiler.Application.Parsing;
using TmCompiler.Domain.Diagnostics;
using TmCompiler.Domain.Lexical;
using TmCompiler.Domain.Syntactic;

namespace TmIde.Services.Analysis;

/// <summary>
/// Lexer → Parser → analizador semántico. Cada fase solo corre si la anterior
/// terminó sin error: el Lexer y el Parser se detienen en el primer error (lanzan
/// excepción) y el análisis semántico necesita el AST completo.
/// </summary>

public sealed class CodeAnalysisService : ICodeAnalysisService
{
    // La especificación no asigna código a los errores léxicos ni sintácticos
    // el CLI del compilador usa el mismo E000 genérico.
    private const string GenericErrorCode = "E000";

    public IReadOnlyList<EditorDiagnostic> Analyze(string text, string fileName)
    {
        try
        {
            var tokens = new Lexer(text, fileName).Tokenize().ToList();
            var program = new Parser(tokens, fileName).Parse();
            var result = new SemanticAnalyzer(program, fileName).Analyze();

            return result.Diagnostics.Select(FromSemantic).ToList();
        }
        catch (LexerException ex)
        {
            return [FromSyntaxError("Error léxico", ex.Message, ex.Line, ex.Column)];
        }
        catch (ParserException ex)
        {
            return [FromSyntaxError("Error sintáctico", ex.Message, ex.Line, ex.Column)];
        }
        catch (Exception)
        {
            // Un fallo interno del compilador no debe tumbar el editor: sin
            // diagnósticos hasta el siguiente cambio de texto.
            return [];
        }
    }

    private static EditorDiagnostic FromSemantic(Diagnostic d) => new(
        d.Severity == DiagnosticSeverity.Warning ? EditorDiagnosticSeverity.Warning : EditorDiagnosticSeverity.Error,
        d.Code,
        d.Title,
        d.Description,
        d.Suggestion,
        d.Line,
        Column: null);

    private static EditorDiagnostic FromSyntaxError(string title, string message, int line, int column) => new(
        EditorDiagnosticSeverity.Error,
        GenericErrorCode,
        title,
        message,
        string.Empty,
        line,
        column);
}
