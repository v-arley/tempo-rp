using System.Text;
using TmCompiler.Domain.Lexical;

namespace TmCompiler.Infrastructure;

/// <summary>
/// Responsable de formatear y emitir diagnósticos del lexer.
/// Aplica SRP: separa la generación de tokens de la presentación de errores.
/// </summary>

public static class LexerDiagnosticReporter
{
    /// <summary>
    /// Genera un reporte completo de la tokenización para fines de depuración.
    /// </summary>
    public static string GenerateTokenReport(IEnumerable<Token> tokens, string fileName)
    {
        var report = new StringBuilder();
        report.AppendLine($"=== Reporte Léxico: {fileName} ===");
        report.AppendLine($"{"Token Type",-20} {"Lexeme",-25} {"Location",-15}");
        report.AppendLine(new string('-', 65));

        foreach (Token token in tokens)
        {
            report.AppendLine(
                $"{token.TokenType,-20} {token.Lexeme,-25} L{token.Line}, C{token.Column}");
        }

        return report.ToString();
    }
}