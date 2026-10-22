using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using TmIde.ViewModels;

namespace TmIde.ViewModels.Panel;

/// <summary>
/// Área inferior con pestañas Output / Run. Output recibe los mensajes de la
/// aplicación y de COMPILE; Run muestra la ejecución del programa.
/// </summary>

public sealed partial class BottomPanelViewModel : ViewModelBase
{
    [ObservableProperty]
    private BottomPaneViewModel _selectedPane;

    public BottomPanelViewModel()
    {
        Output = new OutputPaneViewModel("Output");
        Run = new RunPaneViewModel();

        Panes = new ObservableCollection<BottomPaneViewModel> { Output, Run };
        _selectedPane = Output;
    }

    public ObservableCollection<BottomPaneViewModel> Panes { get; }

    public OutputPaneViewModel Output { get; }

    public RunPaneViewModel Run { get; }

    /// <summary>Escribe una línea en Output y trae ese panel al frente.</summary>
    public void Log(string message)
    {
        Output.AppendLine(message);
        SelectedPane = Output;
    }
}
