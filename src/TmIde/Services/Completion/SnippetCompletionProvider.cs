using System;
using System.Collections.Generic;

namespace TmIde.Services.Completion;

/// <summary>
/// Plantillas de las construcciones de TM: escriben la estructura completa
/// (corchetes, bloque <c>&gt; … &lt;</c>, punto y coma) y dejan el cursor donde
/// va lo primero que hay que completar (<see cref="CompletionSuggestion.CaretMarker"/>).
/// La tabla sigue la gramática de la especificación (§3).
/// </summary>
public sealed class SnippetCompletionProvider : ICompletionProvider
{
    private const string Body = "    ";

    private static readonly IReadOnlyList<CompletionSuggestion> Snippets =
    [
        Snippet("session", "Punto de entrada", $"session >\n{Body}$0\n<"),
        Snippet("compose", "Función", $"void : compose $0() >\n{Body}\n<"),
        Snippet("if", "Condición", $"if [$0] >\n{Body}\n<"),
        Snippet("else", "Alternativa", $"else >\n{Body}$0\n<"),
        Snippet("repeat", "Ciclo while", $"repeat [$0] >\n{Body}\n<"),
        Snippet("loop", "Ciclo for", $"loop [sample i = 0; i < $0; i++] >\n{Body}\n<"),
        Snippet("mix", "Selección", $"mix $0 >\n{Body}case 1: >\n{Body}{Body}\n{Body}<\n{Body}default: >\n{Body}{Body}\n{Body}<\n<"),
        Snippet("output", "Mostrar", "output >> $0;"),
        Snippet("input", "Leer", "input << $0;"),
        Snippet("import", "Importar módulo", "import \"$0.tm\";"),
        Snippet("tempo", "Ritmo en BPM", "tempo $0;"),
        Snippet("pulse", "Ráfaga de partículas", "pulse [$0];"),
    ];

    public IEnumerable<CompletionSuggestion> SuggestionsFor(CompletionContext context) => Snippets;

    // La descripción muestra la plantilla tal como quedará, sin el marcador de cursor.
    private static CompletionSuggestion Snippet(string label, string detail, string template) =>
        new(label, CompletionKind.Snippet, template, detail, template.Replace(CompletionSuggestion.CaretMarker, string.Empty, StringComparison.Ordinal));
}
