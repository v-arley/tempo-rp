namespace TmIde.Services.Navigation;

/// <summary>
/// Navegación entre ventanas: centraliza cómo se abren la ventana principal y
/// los diálogos. Los llamadores entregan un ViewModel; la ventana que lo
/// muestra se resuelve por el tipo registrado en <see cref="FlowService"/>,
/// de modo que nadie fuera del servicio crea vistas.
/// </summary>

public interface IFlowService
{
    /// <summary>
    /// Muestra la ventana principal con <paramref name="viewModel"/> como
    /// contexto. Desde entonces, cerrarla termina la aplicación.
    /// </summary>
    void ShowMain<TViewModel>(TViewModel viewModel)
        where TViewModel : class;

    /// <summary>
    /// Abre una ventana nueva para <paramref name="viewModel"/> como diálogo
    /// modal sobre la ventana activa y espera a que se cierre.
    /// </summary>
    void ShowDialog<TViewModel>(TViewModel viewModel)
        where TViewModel : class;

    /// <summary>Termina la aplicación.</summary>
    void Shutdown();
}
