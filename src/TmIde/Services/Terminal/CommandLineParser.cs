using System.Collections.Generic;
using System.Text;

namespace TmIde.Services.Terminal;

/// <summary>
/// Divide una línea de comando en tokens separando por espacios, respetando
/// tramos entre comillas dobles (<c>mkdir "mi carpeta"</c> → dos tokens). No
/// interpreta escapes ni variables: es el mínimo para un shell básico.
/// </summary>

public static class CommandLineParser
{
    public static IReadOnlyList<string> Parse(string commandLine)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        var insideQuotes = false;
        var tokenStarted = false;

        foreach (var character in commandLine)
        {
            if (character == '"')
            {
                insideQuotes = !insideQuotes;
                tokenStarted = true;
                continue;
            }

            if (char.IsWhiteSpace(character) && !insideQuotes)
            {
                if (tokenStarted)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                    tokenStarted = false;
                }

                continue;
            }

            current.Append(character);
            tokenStarted = true;
        }

        if (tokenStarted)
        {
            tokens.Add(current.ToString());
        }

        return tokens;
    }
}
