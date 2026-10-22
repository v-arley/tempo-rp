using System.Collections.Concurrent;
using System.Threading;
using System.Windows.Threading;

using Tempo.Language.Evaluation;

using TmIde.ViewModels.Panel;

namespace TmIde.Services.Execution;

/// <summary>
/// Entrada de un programa en ejecución desde el panel Run. <c>input &lt;&lt;</c>
/// bloquea el hilo del intérprete hasta que el usuario escribe una línea; la
/// cancelación (botón STOP) lo libera con <see cref="System.OperationCanceledException"/>.
/// </summary>

public sealed class IdeRuntimeInput : IRuntimeInput, System.IDisposable
{
    private readonly RunPaneViewModel _pane;
    private readonly Dispatcher _dispatcher;
    private readonly CancellationToken _cancellationToken;
    private readonly BlockingCollection<string> _lines = new();

    public IdeRuntimeInput(RunPaneViewModel pane, Dispatcher dispatcher, CancellationToken cancellationToken)
    {
        _pane = pane;
        _dispatcher = dispatcher;
        _cancellationToken = cancellationToken;
    }

    public string? ReadLine()
    {
        _dispatcher.Invoke(() => _pane.WaitForInput(onSubmitted: _lines.Add));

        return _lines.Take(_cancellationToken);
    }

    public void Dispose() => _lines.Dispose();
}
