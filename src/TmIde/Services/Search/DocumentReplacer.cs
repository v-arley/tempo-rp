using ICSharpCode.AvalonEdit.Document;

namespace TmIde.Services.Search;

/// <summary>Resultado de <see cref="DocumentReplacer.ReplaceNext"/>: si se reemplazó algo y qué coincidencia sigue.</summary>
public readonly record struct ReplaceStep(bool Replaced, TextMatch? Next);

/// <summary>
/// Aplica reemplazos a un <see cref="TextDocument"/> con un
/// <see cref="TextSearcher"/>. Solo toca el documento recibido (la pestaña
/// activa) y nunca guarda: el usuario decide con Ctrl+S o lo deshace con Ctrl+Z.
/// </summary>
public static class DocumentReplacer
{
    /// <summary>
    /// Paso de "Reemplazar": si la selección es exactamente una coincidencia, la
    /// sustituye; después localiza la siguiente coincidencia para seleccionarla.
    /// Así el primer clic muestra qué se va a cambiar y los siguientes lo cambian.
    /// </summary>
    public static ReplaceStep ReplaceNext(TextDocument document, TextSearcher searcher, string replacement, TextMatch selection)
    {
        var text = document.Text;
        var selectionIsMatch = searcher.FindAll(text).Contains(selection);

        if (!selectionIsMatch)
        {
            return new ReplaceStep(false, searcher.FindNext(text, selection.Offset));
        }

        var replacedBy = searcher.ReplacementFor(text, selection, replacement);
        document.Replace(selection.Offset, selection.Length, replacedBy);

        return new ReplaceStep(true, searcher.FindNext(document.Text, selection.Offset + replacedBy.Length));
    }

    /// <summary>
    /// Sustituye todas las coincidencias en una sola operación de deshacer y
    /// devuelve cuántas fueron. Se recorren de la última a la primera para que
    /// cada reemplazo no desplace las posiciones de las que faltan.
    /// </summary>
    public static int ReplaceAll(TextDocument document, TextSearcher searcher, string replacement)
    {
        var text = document.Text;
        var matches = searcher.FindAll(text);
        if (matches.Count == 0)
        {
            return 0;
        }

        using (document.RunUpdate())
        {
            for (var index = matches.Count - 1; index >= 0; index--)
            {
                var match = matches[index];
                document.Replace(match.Offset, match.Length, searcher.ReplacementFor(text, match, replacement));
            }
        }

        return matches.Count;
    }
}
