namespace TmInterpreter.Application;

/// <summary>
/// Destino de salida utilizado por un programa TM durante su ejecución.
/// </summary>
public interface IRuntimeOutput
{
    void Write(string text);
}