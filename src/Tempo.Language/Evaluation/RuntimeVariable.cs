namespace Tempo.Language.Evaluation;

/// <summary>
/// Representa una variable almacenada durante la ejecución de TM.
/// Conserva tanto su tipo declarado como su valor actual.
/// </summary>
public sealed class RuntimeVariable
{
    public RuntimeVariable(
        string name,
        RuntimeValueKind declaredKind,
        RuntimeValue value,
        bool isConstant)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);

        Name = name;
        DeclaredKind = declaredKind;
        Value = value;
        IsConstant = isConstant;
    }

    public RuntimeVariable(
        string name,
        RuntimeValue value,
        bool isConstant)
        : this(
            name,
            value.Kind,
            value,
            isConstant)
    {
    }

    public string Name { get; }

    public RuntimeValueKind DeclaredKind { get; }

    public RuntimeValue Value { get; private set; }

    public bool IsConstant { get; }

    public void Assign(RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (IsConstant)
        {
            throw new InvalidOperationException(
                $"La constante '{Name}' no puede modificarse.");
        }

        Value = value;
    }
}