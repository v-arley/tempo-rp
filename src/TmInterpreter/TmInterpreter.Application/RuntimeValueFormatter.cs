using System.Globalization;
using TmInterpreter.Domain;

namespace TmInterpreter.Application;

/// <summary>
/// Convierte valores de runtime a la representación textual de TM.
/// </summary>
public static class RuntimeValueFormatter
{
    public static string ToText(RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value.Kind switch
        {
            RuntimeValueKind.Sample =>
                ((int)value.Value!)
                .ToString(CultureInfo.InvariantCulture),

            RuntimeValueKind.Wave =>
                FormatWave((float)value.Value!),

            RuntimeValueKind.Tone =>
                ((char)value.Value!).ToString(),

            RuntimeValueKind.Phrase =>
                (string)value.Value!,

            RuntimeValueKind.Signal =>
                (bool)value.Value!
                    ? "true"
                    : "false",

            RuntimeValueKind.Silence =>
                "silence",

            _ => throw new InvalidOperationException(
                $"El valor '{value.Kind}' no puede imprimirse todavía.")
        };
    }

    private static string FormatWave(float value)
    {
        string text =
            value.ToString(
                "G9",
                CultureInfo.InvariantCulture);

        if (!text.Contains(
                '.',
                StringComparison.Ordinal) &&
            !text.Contains(
                'E',
                StringComparison.OrdinalIgnoreCase))
        {
            text += ".0";
        }

        return text;
    }
}