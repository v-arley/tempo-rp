using System.Windows.Threading;

using Tempo.Language.Evaluation;

using TmIde.ViewModels.Panel;

namespace TmIde.Services.Execution;

/// <summary>
/// Salida de un programa en ejecución hacia el panel Run. El intérprete corre en
/// otro hilo; cada escritura se pasa al hilo de la UI con el <see cref="Dispatcher"/>.
/// </summary>

public sealed class IdeRuntimeOutput : IRuntimeOutput
{
    private readonly RunPaneViewModel _pane;
    private readonly Dispatcher _dispatcher;

    public IdeRuntimeOutput(RunPaneViewModel pane, Dispatcher dispatcher)
    {
        _pane = pane;
        _dispatcher = dispatcher;
    }

    // Invoke (no BeginInvoke) frena al intérprete al ritmo de la UI, y la
    // prioridad Background deja pasar antes la entrada del usuario: un bucle que
    // escribe sin parar no bloquea el botón STOP.
    public void Write(string text) => _dispatcher.Invoke(() => _pane.Write(text), DispatcherPriority.Background);
}
