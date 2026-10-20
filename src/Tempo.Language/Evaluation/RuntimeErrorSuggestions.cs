namespace Tempo.Language.Evaluation;

/// <summary>
/// "Posible solución" de cada error de ejecución R001-R007 (§7.3). Es la fuente
/// única para todos los hosts: la CLI y el IDE muestran la misma sugerencia.
/// </summary>

public static class RuntimeErrorSuggestions
{
    private const string GenericSuggestion = "Revise la operacion que produjo el error.";

    private static readonly Dictionary<string, string> _suggestions =
        new(StringComparer.Ordinal)
        {
            [RuntimeErrorCodes.DivisionByZero] =
                "Verifique que el divisor sea diferente de cero.",

            [RuntimeErrorCodes.IndexOutOfBounds] =
                "Verifique que el indice este dentro de los limites de la coleccion.",

            [RuntimeErrorCodes.ArithmeticOverflow] =
                "Utilice valores que no excedan el rango permitido por el tipo de dato.",

            [RuntimeErrorCodes.InputParseError] =
                "Ingrese un valor compatible con el tipo de dato esperado.",

            [RuntimeErrorCodes.UndefinedFunction] =
                "Verifique que la funcion exista y este correctamente declarada.",

            [RuntimeErrorCodes.InputEndOfFile] =
                "Proporcione los datos de entrada necesarios antes de finalizar la entrada.",

            [RuntimeErrorCodes.InvalidToneInput] =
                "Ingrese un caracter valido para el tipo tone."
        };

    /// <summary>
    /// Sugerencia para el código indicado, o una genérica si el código no es
    /// un error de ejecución conocido.
    /// </summary>
    public static string For(string code)
    {
        if (code is not null && _suggestions.TryGetValue(code, out string? suggestion))
        {
            return suggestion;
        }

        return GenericSuggestion;
    }
}
