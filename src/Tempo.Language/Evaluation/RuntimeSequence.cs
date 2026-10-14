namespace Tempo.Language.Evaluation;

/// <summary>
/// Representa una colección unidimensional mutable de TM.
/// </summary>
public sealed class RuntimeSequence
{
    private readonly List<RuntimeValue> _elements;

    public RuntimeSequence(
        IEnumerable<RuntimeValue> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);

        _elements = new List<RuntimeValue>(elements);
    }

    public int Count => _elements.Count;

    public RuntimeValue Get(int index)
    {
        return _elements[index];
    }

    public void Set(
        int index,
        RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        _elements[index] = value;
    }
}