using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using TmIde.ViewModels;

namespace TmIde.ViewModels.Panel;

/// <summary>
/// Área inferior con pestañas Terminal / Output / Debug Console. Los tres
/// paneles arrancan vacíos.
/// </summary>

public sealed partial class BottomPanelViewModel : ViewModelBase
{
    [ObservableProperty]
    private OutputPaneViewModel _selectedPane;

    public BottomPanelViewModel()
    {
        Terminal = new OutputPaneViewModel("Terminal", prompt: ">");
        Output = new OutputPaneViewModel("Output");

        Panes = new ObservableCollection<OutputPaneViewModel> { Terminal, Output };
        _selectedPane = Terminal;
    }

    public ObservableCollection<OutputPaneViewModel> Panes { get; }

    public OutputPaneViewModel Terminal { get; }

    public OutputPaneViewModel Output { get; }

    /// <summary>Escribe una línea en Output y trae ese panel al frente.</summary>
    public void Log(string message)
    {
        Output.AppendLine(message);
        SelectedPane = Output;
    }
}
