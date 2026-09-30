using TmInterpreter.Application;

namespace TmInterpreter.Infrastructure;

public sealed class ConsoleRuntimeOutput : IRuntimeOutput
{
    public void Write(string text)
    {
        Console.Write(text);
    }
}