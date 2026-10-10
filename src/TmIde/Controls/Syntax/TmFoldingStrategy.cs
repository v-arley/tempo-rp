using System.Collections.Generic;
using System.Linq;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

namespace TmIde.Controls.Syntax;

/// <summary>
/// Calcula los plegados del código TM. Un bloque se abre con <c>&gt;</c> y se
/// cierra con <c>&lt;</c> (gramática del Parser: <c>block = "&gt;", {statement}, "&lt;"</c>).
/// Como ambos símbolos son también operadores relacionales, solo cuentan como
/// delimitador de bloque si <c>&gt;</c> es lo último de código en su línea y
/// <c>&lt;</c> lo primero. También pliega los comentarios de bloque
/// <c>##…##</c> de varias líneas. Los textos dentro de cadenas y comentarios se
/// ignoran, reutilizando <see cref="TmSyntaxScanner"/>.
/// </summary>

public static class TmFoldingStrategy
{
    public static void UpdateFoldings(FoldingManager manager, TextDocument document)
    {
        manager.UpdateFoldings(CreateFoldings(document.Text), firstErrorOffset: -1);
    }

    public static IEnumerable<NewFolding> CreateFoldings(string text)
    {
        var spans = TmSyntaxScanner.Scan(text);
        var foldings = new List<NewFolding>();

        // Comentarios de bloque de varias líneas: se pliega el interior y quedan
        // visibles los dos ## de apertura y cierre.
        foreach (var span in spans.Where(s => s.Kind == TmSyntaxKind.Comment))
        {
            var isBlock = span.Length >= 2 && text[span.Start] == '#' && text[span.Start + 1] == '#';
            if (!isBlock)
            {
                continue;
            }

            var closed = span.Length >= 4 && text[span.Start + span.Length - 1] == '#' && text[span.Start + span.Length - 2] == '#';
            var start = span.Start + 2;
            var end = closed ? span.Start + span.Length - 2 : span.Start + span.Length;
            AddIfMultiline(foldings, text, start, end);
        }

        // Bloques > ... <: se enmascaran comentarios y cadenas para no contar sus símbolos.
        var code = text.ToCharArray();
        foreach (var span in spans.Where(s => s.Kind is TmSyntaxKind.Comment or TmSyntaxKind.String))
        {
            for (var i = span.Start; i < span.Start + span.Length; i++)
            {
                if (code[i] != '\n')
                {
                    code[i] = ' ';
                }
            }
        }

        var openings = new Stack<int>();
        var lineStart = 0;
        while (lineStart <= code.Length)
        {
            var lineEnd = System.Array.IndexOf(code, '\n', lineStart);
            lineEnd = lineEnd < 0 ? code.Length : lineEnd;

            var first = lineStart;
            while (first < lineEnd && char.IsWhiteSpace(code[first]))
            {
                first++;
            }

            var last = lineEnd - 1;
            while (last >= first && char.IsWhiteSpace(code[last]))
            {
                last--;
            }

            if (last >= first)
            {
                if (code[first] == '<' && (first + 1 > last || code[first + 1] != '<'))
                {
                    if (openings.Count > 0)
                    {
                        AddIfMultiline(foldings, text, openings.Pop() + 1, first);
                    }
                }
                else if (code[last] == '>' && (last == first || code[last - 1] != '>'))
                {
                    openings.Push(last);
                }
            }

            lineStart = lineEnd + 1;
        }

        return foldings.OrderBy(f => f.StartOffset).ThenBy(f => f.EndOffset);
    }

    private static void AddIfMultiline(List<NewFolding> foldings, string text, int start, int end)
    {
        if (end > start && text.IndexOf('\n', start, end - start) >= 0)
        {
            foldings.Add(new NewFolding(start, end) { Name = " … " });
        }
    }
}
