using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using TmIde.ViewModels;

namespace TmIde.ViewModels.Panel;

/// <summary>
/// Área inferior con pestañas Terminal / Output. La Terminal es un shell mínimo
/// funcional; Output recibe los mensajes de la aplicación.
/// </summary>

public sealed partial class BottomPanelViewModel : ViewModelBase
{
    [ObservableProperty]
    private BottomPaneViewModel _selectedPane;

    public BottomPanelViewModel()
    {
        Terminal = new TerminalPaneViewModel();
        Output = new OutputPaneViewModel("Output");

        Panes = new ObservableCollection<BottomPaneViewModel> { Terminal, Output };
        _selectedPane = Terminal;
    }

    public ObservableCollection<BottomPaneViewModel> Panes { get; }

    public TerminalPaneViewModel Terminal { get; }

    public OutputPaneViewModel Output { get; }

    /// <summary>Escribe una línea en Output y trae ese panel al frente.</summary>
    public void Log(string message)
    {
        Output.AppendLine(message);
        SelectedPane = Output;
    }
}
