using System;
using System.Collections.Generic;

namespace TmIde.Controls.Syntax;

/// <summary>Categoría de color de un fragmento de código TM.</summary>
public enum TmSyntaxKind
{
    Keyword,
    Type,
    Literal,
    Number,
    String,
    Comment,
}

/// <summary>Fragmento coloreable: posición absoluta en el documento y categoría.</summary>
public readonly record struct TmSyntaxSpan(int Start, int Length, TmSyntaxKind Kind);

/// <summary>
/// Escáner léxico tolerante, solo para colorear: nunca lanza excepciones y
/// trata el código incompleto (cadena sin cerrar, comentario de bloque abierto)
/// extendiendo el fragmento hasta el fin de línea o de documento. No sustituye
/// al Lexer del compilador, que se detiene en el primer error.
/// Reglas según la especificación TM §2: <c>#</c> comentario de línea,
/// <c>##…##</c> comentario de bloque, <c>"…"</c> cadena, <c>'…'</c> carácter.
/// </summary>

public static class TmSyntaxScanner
{
    private static readonly Dictionary<string, TmSyntaxKind> Words = new(StringComparer.Ordinal)
    {
        ["compose"] = TmSyntaxKind.Keyword,
        ["session"] = TmSyntaxKind.Keyword,
        ["output"] = TmSyntaxKind.Keyword,
        ["input"] = TmSyntaxKind.Keyword,
        ["mix"] = TmSyntaxKind.Keyword,
        ["loop"] = TmSyntaxKind.Keyword,
        ["repeat"] = TmSyntaxKind.Keyword,
        ["if"] = TmSyntaxKind.Keyword,
        ["else"] = TmSyntaxKind.Keyword,
        ["ret"] = TmSyntaxKind.Keyword,
        ["import"] = TmSyntaxKind.Keyword,
        ["const"] = TmSyntaxKind.Keyword,
        ["sequence"] = TmSyntaxKind.Keyword,
        ["score"] = TmSyntaxKind.Keyword,
        ["and"] = TmSyntaxKind.Keyword,
        ["or"] = TmSyntaxKind.Keyword,
        ["not"] = TmSyntaxKind.Keyword,
        ["sample"] = TmSyntaxKind.Type,
        ["wave"] = TmSyntaxKind.Type,
        ["tone"] = TmSyntaxKind.Type,
        ["phrase"] = TmSyntaxKind.Type,
        ["signal"] = TmSyntaxKind.Type,
        ["void"] = TmSyntaxKind.Type,
        ["true"] = TmSyntaxKind.Literal,
        ["false"] = TmSyntaxKind.Literal,
        ["silence"] = TmSyntaxKind.Literal,
    };

    public static IReadOnlyList<TmSyntaxSpan> Scan(string text)
    {
        var spans = new List<TmSyntaxSpan>();
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (c == '#')
            {
                var start = i;
                if (i + 1 < text.Length && text[i + 1] == '#')
                {
                    var close = text.IndexOf("##", i + 2, StringComparison.Ordinal);
                    i = close < 0 ? text.Length : close + 2;
                }
                else
                {
                    i = EndOfLine(text, i);
                }

                spans.Add(new TmSyntaxSpan(start, i - start, TmSyntaxKind.Comment));
            }
            else if (c == '"' || c == '\'')
            {
                var start = i;
                i = EndOfQuoted(text, i, c);
                spans.Add(new TmSyntaxSpan(start, i - start, TmSyntaxKind.String));
            }
            else if (char.IsAsciiDigit(c))
            {
                var start = i;
                while (i < text.Length && char.IsAsciiDigit(text[i]))
                {
                    i++;
                }

                if (i + 1 < text.Length && text[i] == '.' && char.IsAsciiDigit(text[i + 1]))
                {
                    i++;
                    while (i < text.Length && char.IsAsciiDigit(text[i]))
                    {
                        i++;
                    }
                }

                spans.Add(new TmSyntaxSpan(start, i - start, TmSyntaxKind.Number));
            }
            else if (char.IsLetter(c) || c == '_')
            {
                var start = i;
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
                {
                    i++;
                }

                if (Words.TryGetValue(text.Substring(start, i - start), out var kind))
                {
                    spans.Add(new TmSyntaxSpan(start, i - start, kind));
                }
            }
            else
            {
                i++;
            }
        }

        return spans;
    }

    private static int EndOfLine(string text, int from)
    {
        var end = text.IndexOf('\n', from);
        return end < 0 ? text.Length : end;
    }

    // Cadena o carácter: termina en la comilla de cierre (con escapes \x) o, si
    // falta, al final de la línea.
    private static int EndOfQuoted(string text, int open, char quote)
    {
        var i = open + 1;
        while (i < text.Length && text[i] != '\n')
        {
            if (text[i] == '\\' && i + 1 < text.Length && text[i + 1] != '\n')
            {
                i += 2;
                continue;
            }

            if (text[i] == quote)
            {
                return i + 1;
            }

            i++;
        }

        return i;
    }
}
