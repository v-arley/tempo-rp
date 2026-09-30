namespace TmInterpreter.Domain;

/// <summary>
/// Excepción producida durante la ejecución de un programa TM.
/// </summary>
public sealed class TmRuntimeException : Exception
{
    public TmRuntimeException(
        string code,
        int line,
        string message)
        : base(message)
    {
        Code = code;
        Line = line;
    }

    public string Code { get; }

    public int Line { get; }
}