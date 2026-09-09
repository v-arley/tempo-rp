namespace TmIde.ViewModels.Panel;

/// <summary>Panel de solo lectura del área inferior (Output): recibe mensajes de la aplicación.</summary>

public sealed class OutputPaneViewModel : BottomPaneViewModel
{
    public OutputPaneViewModel(string header)
        : base(header)
    {
    }
}
