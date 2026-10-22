using System.Collections.ObjectModel;

using TmIde.ViewModels;

namespace TmIde.ViewModels.Panel;

/// <summary>
/// Base de un panel del área inferior (Output, Run): una cabecera y un
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

    /// <summary>
    /// Añade el texto al final; si tiene saltos de línea (un diagnóstico de la
    /// §7.1), cada línea es una entrada, como espera <c>LogView</c>.
    /// </summary>
    public void AppendLine(string text)
    {
        foreach (var line in text.ReplaceLineEndings("\n").Split('\n'))
        {
            Lines.Add(line);
        }
    }
}
