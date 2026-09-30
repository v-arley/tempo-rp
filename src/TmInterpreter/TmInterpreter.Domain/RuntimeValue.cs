namespace TmInterpreter.Domain;

/// <summary>
/// Representa un valor existente durante la ejecución de un programa TM.
/// </summary>
public sealed record RuntimeValue(
    RuntimeValueKind Kind,
    object? Value)
{
    public static RuntimeValue Sample(int value)
    {
        return new RuntimeValue(RuntimeValueKind.Sample, value);
    }

    public static RuntimeValue Wave(float value)
    {
        return new RuntimeValue(RuntimeValueKind.Wave, value);
    }

    public static RuntimeValue Tone(char value)
    {
        return new RuntimeValue(RuntimeValueKind.Tone, value);
    }

    public static RuntimeValue Phrase(string value)
    {
        return new RuntimeValue(RuntimeValueKind.Phrase, value);
    }

    public static RuntimeValue Signal(bool value)
    {
        return new RuntimeValue(RuntimeValueKind.Signal, value);
    }

    public static RuntimeValue Silence()
    {
        return new RuntimeValue(RuntimeValueKind.Silence, null);
    }

    public static RuntimeValue Void()
    {
        return new RuntimeValue(RuntimeValueKind.Void, null);
    }
}