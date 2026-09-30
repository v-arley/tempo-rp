namespace TmInterpreter.Domain;

/// <summary>
/// Mantiene y supervisa los marcos de activación
/// durante las llamadas a funciones TM.
/// </summary>
public sealed class RuntimeCallTracker
{
    private readonly Stack<RuntimeCallFrame> _frames = new();

    public int Depth => _frames.Count;

    public int MaximumDepth { get; private set; }

    public RuntimeCallFrame? Current =>
        _frames.Count == 0
            ? null
            : _frames.Peek();

    public void Push(RuntimeCallFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        _frames.Push(frame);

        if (_frames.Count > MaximumDepth)
        {
            MaximumDepth = _frames.Count;
        }
    }

    public RuntimeCallFrame Pop()
    {
        if (_frames.Count == 0)
        {
            throw new InvalidOperationException(
                "La pila de llamadas está vacía.");
        }

        return _frames.Pop();
    }
}