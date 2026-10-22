using Tempo.Language.Evaluation;
using Tempo.Language.Tests.Projects;
using Xunit;

namespace Tempo.Language.Tests.Evaluation;

/// <summary>
/// <see cref="Compilation.Run"/> con un <see cref="CancellationToken"/>: el host
/// (el botón de detener del IDE) corta incluso un bucle infinito.
/// </summary>

public sealed class RunCancellationTests
{
    [Fact]
    public void Run_CancelledDuringExecution_StopsBeforeTheNextStatement()
    {
        using var project = ProjectWorkspace.With(("principal.tmpr", """
            session >
                repeat [true] >
                    output >> "x";
                <
            <
            """));
        Compilation compilation = Compile(project);

        using var cancellation = new CancellationTokenSource();
        var output = new CancelAfterWrites(cancellation, writes: 3);

        Assert.Throws<OperationCanceledException>(
            () => compilation.Run(new NoInput(), output, cancellation.Token));
        Assert.Equal(3, output.Writes);
    }

    [Fact]
    public async Task Run_Cancelled_StopsALoopWithAnEmptyBody()
    {
        using var project = ProjectWorkspace.With(("principal.tmpr", """
            session >
                repeat [true] >
                <
            <
            """));
        Compilation compilation = Compile(project);

        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        Task run = Task.Run(() => compilation.Run(new NoInput(), new NoOutput(), cancellation.Token));

        // Si la cancelación no llega al bucle, la ejecución nunca termina.
        Task finished = await Task.WhenAny(run, Task.Delay(TimeSpan.FromSeconds(10)));
        Assert.Same(run, finished);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
    }

    private static Compilation Compile(ProjectWorkspace project)
    {
        Compilation compilation = project.CompileDirectory();
        Assert.True(compilation.IsExecutable, string.Join(Environment.NewLine, compilation.Diagnostics));
        return compilation;
    }

    private sealed class NoInput : IRuntimeInput
    {
        public string? ReadLine()
        {
            return null;
        }
    }

    private sealed class NoOutput : IRuntimeOutput
    {
        public void Write(string text)
        {
        }
    }

    // Cancela la ejecución al llegar a cierto número de escrituras.
    private sealed class CancelAfterWrites(CancellationTokenSource cancellation, int writes) : IRuntimeOutput
    {
        public int Writes { get; private set; }

        public void Write(string text)
        {
            Writes++;
            if (Writes == writes)
            {
                cancellation.Cancel();
            }
        }
    }
}
