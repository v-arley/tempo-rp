namespace Tempo.Language.Symbols;

/// <summary>
/// Traduce el nombre de tipo que el parser deja en el AST como cadena
/// (por ejemplo <c>"score&lt;sample&gt;"</c>) al modelo <see cref="TmType"/>.
/// Es el único punto del compilador que interpreta esas cadenas.
/// </summary>

public static class TypeNameParser
{
    private static readonly Dictionary<string, TmType> PrimitiveTypes =
        new(StringComparer.Ordinal)
        {
            ["sample"] = TmType.Sample,
            ["wave"] = TmType.Wave,
            ["tone"] = TmType.Tone,
            ["phrase"] = TmType.Phrase,
            ["signal"] = TmType.Signal,
            ["silence"] = TmType.Silence,
            ["void"] = TmType.Void
        };

    public static bool TryParse(string typeName, out TmType type)
    {
        type = TmType.Unknown;

        if (string.IsNullOrWhiteSpace(typeName))
        {
            return false;
        }

        string normalized = typeName.Trim();

        if (PrimitiveTypes.TryGetValue(normalized, out TmType primitive))
        {
            type = primitive;
            return true;
        }

        int openingAngle = normalized.IndexOf('<');

        if (openingAngle < 0 || !normalized.EndsWith(">", StringComparison.Ordinal))
        {
            return false;
        }

        string head = normalized[..openingAngle].Trim();
        string argument = normalized[(openingAngle + 1)..^1];

        if (!TryParse(argument, out TmType elementType))
        {
            return false;
        }

        switch (head)
        {
            case "sequence":
                type = TmType.SequenceOf(elementType);
                return true;

            case "score":
                type = TmType.ScoreOf(elementType);
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Igual que <see cref="TryParse"/>, pero devuelve <see cref="TmType.Unknown"/>
    /// en lugar de un booleano. <c>Unknown</c> se propaga en silencio, así que un
    /// nombre de tipo irreconocible no genera diagnósticos derivados.
    /// </summary>
    public static TmType ParseOrUnknown(string typeName)
    {
        return TryParse(typeName, out TmType type) ? type : TmType.Unknown;
    }
}
