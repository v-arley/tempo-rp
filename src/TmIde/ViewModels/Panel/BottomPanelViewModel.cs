using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Tempo.Language.Diagnostics;
using TmIde.ViewModels;

namespace TmIde.ViewModels.Panel;

/// Área inferior con las pestañas Output y Run.
public sealed partial class BottomPanelViewModel : ViewModelBase
{
    [ObservableProperty]
    private BottomPaneViewModel _selectedPane;

    public BottomPanelViewModel()
    {
        Output = new OutputPaneViewModel("Output");
        Run = new RunPaneViewModel();

        Panes = new ObservableCollection<BottomPaneViewModel>
        {
            Output,
            Run
        };

        _selectedPane = Output;
    }

    public ObservableCollection<BottomPaneViewModel> Panes { get; }

    public OutputPaneViewModel Output { get; }

    public RunPaneViewModel Run { get; }

    /// Escribe un mensaje normal y muestra Output
    public void Log(string message)
    {
        Output.AppendLine(message);
        SelectedPane = Output;
    }

    /// Escribe un diagnóstico navegable y muestra Output
    public void LogDiagnostic(
        Diagnostic diagnostic,
        string? filePath)
    {
        Output.AppendDiagnostic(diagnostic, filePath);
        SelectedPane = Output;
    }
    
    /// Muestra un diagnostico con encabezado de categoria y conserva su enlace de navegacion
    public void LogCategorizedDiagnostic(
        Diagnostic diagnostic,
        string? filePath)
    {
        Output.AppendCategorizedDiagnostic(
            diagnostic,
            filePath);

        SelectedPane = Output;
    }

}
