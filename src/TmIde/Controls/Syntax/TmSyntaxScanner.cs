using System;
using System.Collections.Generic;

namespace TmIde.Controls.Syntax;

/// <summary>
/// Escáner léxico tolerante, solo para colorear: nunca lanza excepciones y
/// trata el código incompleto (cadena sin cerrar, comentario de bloque abierto)
/// extendiendo el fragmento hasta el fin de línea o de documento. No sustituye
/// al Lexer del compilador, que se detiene en el primer error.
/// Reglas léxicas de TM: <c>#</c> comentario de línea,
/// <c>##…##</c> comentario de bloque, <c>"…"</c> cadena, <c>'…'</c> carácter.
/// </summary>

public static class TmSyntaxScanner
{
    private const string BlockCommentDelimiter = "##";

    private static readonly Dictionary<string, TmSyntaxKind> ReservedWordKinds = new(StringComparer.Ordinal)
    {
        ["compose"] = TmSyntaxKind.Keyword,
        ["session"] = TmSyntaxKind.Keyword,
        ["output"] = TmSyntaxKind.Keyword,
        ["input"] = TmSyntaxKind.Keyword,
        ["mix"] = TmSyntaxKind.Keyword,
        ["case"] = TmSyntaxKind.Keyword,
        ["default"] = TmSyntaxKind.Keyword,
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
        ["tempo"] = TmSyntaxKind.Keyword,
        ["pulse"] = TmSyntaxKind.Keyword,
        ["sample"] = TmSyntaxKind.Type,
        ["wave"] = TmSyntaxKind.Type,
        ["tone"] = TmSyntaxKind.Type,
        ["phrase"] = TmSyntaxKind.Type,
        ["signal"] = TmSyntaxKind.Type,
        ["void"] = TmSyntaxKind.Type,
        ["true"] = TmSyntaxKind.BuiltInValue,
        ["false"] = TmSyntaxKind.BuiltInValue,
        ["silence"] = TmSyntaxKind.BuiltInValue,
    };

    public static IReadOnlyList<TmSyntaxSpan> Scan(string text)
    {
        var spans = new List<TmSyntaxSpan>();
        var position = 0;
        while (position < text.Length)
        {
            var start = position;
            var current = text[position];

            if (current == '#')
            {
                position = EndOfComment(text, start);
                spans.Add(new TmSyntaxSpan(start, position - start, TmSyntaxKind.Comment));
            }
            else if (current is '"' or '\'')
            {
                position = EndOfQuoted(text, start, quote: current);
                spans.Add(new TmSyntaxSpan(start, position - start, TmSyntaxKind.StringLiteral));
            }
            else if (char.IsAsciiDigit(current))
            {
                position = EndOfNumber(text, start);
                spans.Add(new TmSyntaxSpan(start, position - start, TmSyntaxKind.Number));
            }
            else if (IsWordStart(current))
            {
                position = EndOfWord(text, start);
                if (ReservedWordKinds.TryGetValue(text[start..position], out var kind))
                {
                    spans.Add(new TmSyntaxSpan(start, position - start, kind));
                }
            }
            else
            {
                position++;
            }
        }

        return spans;
    }

    public static bool IsWordStart(char value) => char.IsLetter(value) || value == '_';

    public static bool IsWordPart(char value) => char.IsLetterOrDigit(value) || value == '_';

    // "##" abre un comentario de bloque que llega hasta el "##" de cierre (o el
    // fin del documento si falta); un "#" suelto comenta hasta el fin de línea.
    private static int EndOfComment(string text, int start)
    {
        if (!text.AsSpan(start).StartsWith(BlockCommentDelimiter, StringComparison.Ordinal))
        {
            return EndOfLine(text, start);
        }

        var close = text.IndexOf(BlockCommentDelimiter, start + BlockCommentDelimiter.Length, StringComparison.Ordinal);
        return close < 0 ? text.Length : close + BlockCommentDelimiter.Length;
    }

    // Cadena o carácter: termina en la comilla de cierre (con escapes \x) o, si
    // falta, al final de la línea.
    private static int EndOfQuoted(string text, int start, char quote)
    {
        var position = start + 1;
        while (position < text.Length && text[position] != '\n')
        {
            var isEscape = text[position] == '\\' && position + 1 < text.Length && text[position + 1] != '\n';
            if (isEscape)
            {
                position += 2;
                continue;
            }

            if (text[position] == quote)
            {
                return position + 1;
            }

            position++;
        }

        return position;
    }

    // Entero o decimal: dígitos, y opcionalmente '.' seguido de más dígitos.
    private static int EndOfNumber(string text, int start)
    {
        var position = EndOfDigits(text, start);
        var hasFraction = position + 1 < text.Length && text[position] == '.' && char.IsAsciiDigit(text[position + 1]);

        return hasFraction ? EndOfDigits(text, position + 1) : position;
    }

    private static int EndOfDigits(string text, int start)
    {
        var position = start;
        while (position < text.Length && char.IsAsciiDigit(text[position]))
        {
            position++;
        }

        return position;
    }

    private static int EndOfWord(string text, int start)
    {
        var position = start;
        while (position < text.Length && IsWordPart(text[position]))
        {
            position++;
        }

        return position;
    }

    private static int EndOfLine(string text, int start)
    {
        var end = text.IndexOf('\n', start);
        return end < 0 ? text.Length : end;
    }
}
