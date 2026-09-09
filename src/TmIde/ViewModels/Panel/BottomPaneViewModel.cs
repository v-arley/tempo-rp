using System.Collections.ObjectModel;

using TmIde.ViewModels;

namespace TmIde.ViewModels.Panel;

/// <summary>
/// Base de un panel del área inferior (Output, Terminal): una cabecera y un
/// búfer de líneas emitidas.
/// </summary>

public abstract class BottomPaneViewModel : ViewModelBase
{
    protected BottomPaneViewModel(string header)
    {
        Header = header;
    }

    public string Header { get; }

    public ObservableCollection<string> Lines { get; } = new();

    public void AppendLine(string line) => Lines.Add(line);
}
