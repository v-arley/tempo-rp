using Tempo.Language.Evaluation;

namespace Tempo.Cli;

public sealed class ConsoleRuntimeOutput : IRuntimeOutput
{
    public void Write(string text)
    {
        Console.Write(text);
    }
}