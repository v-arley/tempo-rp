using TmInterpreter.Application;

namespace TmInterpreter.Infrastructure;

public sealed class ConsoleRuntimeInput : IRuntimeInput
{
    public string? ReadLine()
    {
        return Console.ReadLine();
    }
}