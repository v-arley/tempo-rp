namespace TmInterpreter.Domain;

/// <summary>
/// Representa un marco de activación de una función TM.
/// </summary>
public sealed class RuntimeCallFrame
{
    public RuntimeCallFrame(
        string functionName,
        RuntimeEnvironment environment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(functionName);
        ArgumentNullException.ThrowIfNull(environment);

        FunctionName = functionName;
        Environment = environment;
    }

    public string FunctionName { get; }

    public RuntimeEnvironment Environment { get; }
}