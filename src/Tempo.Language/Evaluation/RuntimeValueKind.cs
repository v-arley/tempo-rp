namespace Tempo.Language.Evaluation;

/// <summary>
/// Tipos de valores que pueden existir durante la ejecución de TM.
/// </summary>
public enum RuntimeValueKind
{
    Sample,
    Wave,
    Tone,
    Phrase,
    Signal,
    Silence,
    Sequence,
    Score,
    Void
}