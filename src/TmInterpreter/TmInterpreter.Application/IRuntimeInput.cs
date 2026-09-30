namespace TmInterpreter.Application;

/// <summary>
/// Fuente de entrada utilizada por un programa TM durante su ejecución.
/// </summary>
public interface IRuntimeInput
{
    string? ReadLine();
}