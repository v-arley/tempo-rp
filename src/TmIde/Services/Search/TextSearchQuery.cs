using System;
using System.Text.RegularExpressions;

namespace TmIde.Services.Search;

/// <summary>
/// Lo que el usuario pidió buscar, tal como lo escribió en la barra. Es un
/// valor inmutable: cambiar una opción produce otra consulta. Se convierte en
/// un <see cref="TextSearcher"/> con <see cref="TryCompile"/>.
/// </summary>
/// <param name="Pattern">Texto o expresión regular a buscar.</param>
/// <param name="MatchCase">Distingue mayúsculas de minúsculas.</param>
/// <param name="WholeWord">Solo coincidencias que no estén pegadas a otra letra, dígito o '_'.</param>
/// <param name="UseRegex">Interpreta <paramref name="Pattern"/> como expresión regular de .NET.</param>
public sealed record TextSearchQuery(
    string Pattern,
    bool MatchCase = false,
    bool WholeWord = false,
    bool UseRegex = false)
{
    // Un patrón patológico no debe congelar el editor.
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    // Bordes de palabra con la regla de los identificadores de TM (letra, dígito
    // o '_'); \b de .NET usa otra definición de "palabra".
    private const string WordBoundaryBefore = @"(?<![\p{L}\p{N}_])";
    private const string WordBoundaryAfter = @"(?![\p{L}\p{N}_])";

    public bool IsEmpty => string.IsNullOrEmpty(Pattern);

    /// <summary>
    /// Traduce la consulta a un buscador. Devuelve <c>false</c> si la consulta
    /// está vacía o la expresión regular no es válida; en ese caso
    /// <paramref name="error"/> explica por qué (vacío si solo faltaba texto).
    /// </summary>
    public bool TryCompile(out TextSearcher searcher, out string error)
    {
        searcher = TextSearcher.Nothing;
        error = string.Empty;

        if (IsEmpty)
        {
            return false;
        }

        try
        {
            searcher = new TextSearcher(new Regex(BuildExpression(), BuildOptions(), MatchTimeout), UseRegex);
            return true;
        }
        catch (ArgumentException exception)
        {
            error = exception.Message;
            return false;
        }
    }

    // Toda consulta se vuelve una expresión regular: el texto literal se escapa
    // y la palabra completa se rodea de bordes. Así un solo motor atiende las
    // tres opciones y sus combinaciones.
    private string BuildExpression()
    {
        var expression = UseRegex ? Pattern : Regex.Escape(Pattern);

        return WholeWord
            ? $"{WordBoundaryBefore}(?:{expression}){WordBoundaryAfter}"
            : expression;
    }

    private RegexOptions BuildOptions() =>
        RegexOptions.CultureInvariant | RegexOptions.Multiline | (MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase);
}
