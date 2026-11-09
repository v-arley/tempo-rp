using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TmIde.Services.Search;

/// <summary>Una coincidencia: posición absoluta en el texto y su longitud.</summary>
public readonly record struct TextMatch(int Offset, int Length)
{
    public int End => Offset + Length;
}

/// <summary>
/// Una consulta ya compilada (<see cref="TextSearchQuery.TryCompile"/>). Busca
/// sobre una copia del texto, sin conocer el editor, y calcula el texto con el
/// que se reemplaza cada coincidencia. Las coincidencias vacías (por ejemplo
/// <c>^</c>) se descartan: no se pueden señalar ni reemplazar de forma útil.
/// <see cref="Nothing"/> es el buscador que nunca encuentra nada (Null Object).
/// </summary>
public sealed class TextSearcher
{
    private readonly Regex? _regex;
    private readonly bool _expandsReplacement;

    internal TextSearcher(Regex regex, bool expandsReplacement)
    {
        _regex = regex;
        _expandsReplacement = expandsReplacement;
    }

    private TextSearcher()
    {
    }

    /// <summary>Buscador sin coincidencias, para consultas vacías o inválidas.</summary>
    public static TextSearcher Nothing { get; } = new();

    /// <summary>Todas las coincidencias, en orden. Si la búsqueda excede el tiempo límite se devuelven las halladas hasta entonces.</summary>
    public IReadOnlyList<TextMatch> FindAll(string text)
    {
        var matches = new List<TextMatch>();
        if (_regex is null)
        {
            return matches;
        }

        try
        {
            foreach (Match match in _regex.Matches(text))
            {
                if (match.Length > 0)
                {
                    matches.Add(new TextMatch(match.Index, match.Length));
                }
            }
        }
        catch (RegexMatchTimeoutException)
        {
            // Expresión demasiado costosa: se conserva lo encontrado.
        }

        return matches;
    }

    /// <summary>Primera coincidencia que empieza en <paramref name="offset"/> o después; si no hay, vuelve al principio.</summary>
    public TextMatch? FindNext(string text, int offset)
    {
        var matches = FindAll(text);

        return matches.Count == 0
            ? null
            : matches.FirstOrDefault(match => match.Offset >= offset, matches[0]);
    }

    /// <summary>
    /// Texto que sustituye a <paramref name="match"/>. Con expresión regular,
    /// <paramref name="replacement"/> admite grupos (<c>$1</c>, <c>${nombre}</c>);
    /// sin ella se inserta literalmente.
    /// </summary>
    public string ReplacementFor(string text, TextMatch match, string replacement)
    {
        if (_regex is null || !_expandsReplacement)
        {
            return replacement;
        }

        try
        {
            var found = _regex.Match(text, match.Offset);
            return found.Success && found.Index == match.Offset ? found.Result(replacement) : replacement;
        }
        catch (RegexMatchTimeoutException)
        {
            return replacement;
        }
    }
}
