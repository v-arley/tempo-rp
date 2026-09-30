namespace TmInterpreter.Domain;

/// <summary>
/// Representa un ámbito de variables durante la ejecución.
/// Los ámbitos pueden estar anidados mediante Parent.
/// </summary>
public sealed class RuntimeEnvironment
{
    private readonly Dictionary<string, RuntimeVariable> _variables =
        new(StringComparer.Ordinal);

    public RuntimeEnvironment(RuntimeEnvironment? parent = null)
    {
        Parent = parent;
    }

    public RuntimeEnvironment? Parent { get; }

    public void Declare(
        string name,
        RuntimeValue value,
        bool isConstant = false)
    {
        Declare(
            name,
            value.Kind,
            value,
            isConstant);
    }

    public void Declare(
        string name,
        RuntimeValueKind declaredKind,
        RuntimeValue value,
        bool isConstant = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);

        if (_variables.ContainsKey(name))
        {
            throw new InvalidOperationException(
                $"La variable '{name}' ya existe en este ámbito.");
        }

        _variables.Add(
            name,
            new RuntimeVariable(
                name,
                declaredKind,
                value,
                isConstant));
    }

    public bool TryResolve(
        string name,
        out RuntimeVariable? variable)
    {
        if (_variables.TryGetValue(name, out variable))
        {
            return true;
        }

        if (Parent is not null)
        {
            return Parent.TryResolve(name, out variable);
        }

        variable = null;
        return false;
    }

    public RuntimeVariable Resolve(string name)
    {
        if (TryResolve(name, out RuntimeVariable? variable) &&
            variable is not null)
        {
            return variable;
        }

        throw new InvalidOperationException(
            $"La variable '{name}' no existe en el entorno de ejecución.");
    }

    public void Assign(
        string name,
        RuntimeValue value)
    {
        RuntimeVariable variable = Resolve(name);
        variable.Assign(value);
    }
}