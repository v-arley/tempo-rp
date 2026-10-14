using Tempo.Language.Evaluation;

namespace Tempo.Cli;

public sealed class ConsoleRuntimeInput : IRuntimeInput
{
    public string? ReadLine()
    {
        return Console.ReadLine();
    }
}