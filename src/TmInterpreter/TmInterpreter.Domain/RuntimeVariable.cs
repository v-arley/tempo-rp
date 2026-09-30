namespace TmInterpreter.Domain;

/// <summary>
/// Variable almacenada durante la ejecución.
/// </summary>
public sealed class RuntimeVariable
{
    public RuntimeVariable(
        string name,
        RuntimeValue value,
        bool isConstant)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);

        Name = name;
        Value = value;
        IsConstant = isConstant;
    }

    public string Name { get; }

    public RuntimeValue Value { get; set; }

    public bool IsConstant { get; }
}