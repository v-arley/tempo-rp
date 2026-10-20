using System;

using TmIde.Controls.Syntax;

namespace TmIde.Services.Analysis;

/// <summary>
/// Traduce la posición de un diagnóstico (línea y, si existe, columna) a un
/// rango de offsets del texto. Con columna se subraya el fragmento que empieza
/// ahí (el token, según <see cref="TmSyntaxScanner"/>, o una palabra o un
/// carácter si no es un token coloreable); sin columna, la línea completa sin
/// sangría ni espacios finales. Función pura: no depende de la UI.
/// </summary>

public static class DiagnosticRangeResolver
{
    /// <summary>Devuelve el rango, o <c>null</c> si la línea no existe en el texto.</summary>
    public static (int Start, int Length)? Resolve(string text, EditorDiagnostic diagnostic)
    {
        if (diagnostic.Line < 1)
        {
            return null;
        }

        var lineStart = 0;
        for (var line = 1; line < diagnostic.Line; line++)
        {
            var next = text.IndexOf('\n', lineStart);
            if (next < 0)
            {
                return null;
            }

            lineStart = next + 1;
        }

        var lineEnd = text.IndexOf('\n', lineStart);
        lineEnd = lineEnd < 0 ? text.Length : lineEnd;
        if (lineEnd > lineStart && text[lineEnd - 1] == '\r')
        {
            lineEnd--;
        }

        return diagnostic.Column is { } column
            ? ResolveAtColumn(text, lineStart, lineEnd, column)
            : ResolveWholeLine(text, lineStart, lineEnd);
    }

    private static (int Start, int Length)? ResolveWholeLine(string text, int lineStart, int lineEnd)
    {
        var start = lineStart;
        while (start < lineEnd && char.IsWhiteSpace(text[start]))
        {
            start++;
        }

        var end = lineEnd;
        while (end > start && char.IsWhiteSpace(text[end - 1]))
        {
            end--;
        }

        // Línea vacía: nada que subrayar.
        return end > start ? (start, end - start) : null;
    }

    private static (int Start, int Length)? ResolveAtColumn(string text, int lineStart, int lineEnd, int column)
    {
        if (lineEnd == lineStart)
        {
            return null;
        }

        // Una columna pasada del final (error "al llegar al fin de línea") se
        // ancla al último carácter de la línea.
        var offset = Math.Min(lineStart + Math.Max(column, 1) - 1, lineEnd - 1);

        foreach (var span in TmSyntaxScanner.Scan(text[lineStart..lineEnd]))
        {
            var start = lineStart + span.Start;
            if (offset >= start && offset < start + span.Length)
            {
                return (start, span.Length);
            }
        }

        var end = offset;
        while (end < lineEnd && (char.IsLetterOrDigit(text[end]) || text[end] == '_'))
        {
            end++;
        }

        return (offset, Math.Max(end - offset, 1));
    }
}
