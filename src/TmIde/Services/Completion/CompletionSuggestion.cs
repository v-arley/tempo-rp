using System;

namespace TmIde.Services.Completion;

/// <summary>
/// Clase de sugerencia. El orden de declaración es la prioridad con la que se
/// listan cuando dos sugerencias coinciden igual de bien con lo escrito: lo
/// declarado por el usuario va antes que el vocabulario del lenguaje.
/// </summary>
public enum CompletionKind
{
    Variable,
    Parameter,
    Constant,
    Function,
    Keyword,
    Snippet,
    Type,
    Literal,

    /// <summary>Palabra escrita en el documento que el compilador aún no reconoce como símbolo.</summary>
    Word,
}

/// <summary>Texto que se inserta y dónde queda el cursor dentro de él.</summary>
public readonly record struct CompletionInsertion(string Text, int CaretIndex);

/// <summary>
/// Una entrada de la lista de autocompletado.
/// </summary>
/// <param name="Label">Lo que se muestra y con lo que se filtra.</param>
/// <param name="Kind">Clase de la sugerencia (color, orden).</param>
/// <param name="InsertText">
/// Texto que sustituye a lo escrito. Puede contener <see cref="CaretMarker"/>
/// para indicar dónde queda el cursor y saltos de línea, que heredan la sangría
/// de la línea actual.
/// </param>
/// <param name="Detail">Resumen breve junto a la etiqueta (tipo, firma, equivalente).</param>
/// <param name="Description">Explicación que se muestra al lado de la lista.</param>
public sealed record CompletionSuggestion(
    string Label,
    CompletionKind Kind,
    string InsertText,
    string Detail = "",
    string Description = "")
{
    /// <summary>Marca, dentro de <see cref="InsertText"/>, la posición final del cursor.</summary>
    public const string CaretMarker = "$0";

    /// <summary>
    /// Prepara el texto para insertarlo en una línea con sangría
    /// <paramref name="indentation"/>: cada salto de línea la repite y el
    /// marcador de cursor se convierte en una posición.
    /// </summary>
    public CompletionInsertion ExpandFor(string indentation)
    {
        var text = InsertText.Replace("\n", "\n" + indentation, StringComparison.Ordinal);
        var caretIndex = text.IndexOf(CaretMarker, StringComparison.Ordinal);

        return caretIndex < 0
            ? new CompletionInsertion(text, text.Length)
            : new CompletionInsertion(text.Remove(caretIndex, CaretMarker.Length), caretIndex);
    }
}
