namespace Tempo.Language.Evaluation;

/// <summary>
/// Convierte los nombres de tipos producidos por el Parser
/// a tipos utilizados durante la ejecución.
/// </summary>
public static class RuntimeTypeResolver
{
    public static RuntimeValueKind Resolve(string typeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);

        return typeName switch
        {
            "sample" => RuntimeValueKind.Sample,
            "wave" => RuntimeValueKind.Wave,
            "tone" => RuntimeValueKind.Tone,
            "phrase" => RuntimeValueKind.Phrase,
            "signal" => RuntimeValueKind.Signal,
            "silence" => RuntimeValueKind.Silence,
            "void" => RuntimeValueKind.Void,

            _ when typeName.StartsWith(
                "sequence<",
                StringComparison.Ordinal) =>
                RuntimeValueKind.Sequence,

            _ when typeName.StartsWith(
                "score<",
                StringComparison.Ordinal) =>
                RuntimeValueKind.Score,

            _ => throw new InvalidOperationException(
                $"Tipo TM desconocido: '{typeName}'.")
        };
    }
}