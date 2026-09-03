using System.Collections.ObjectModel;

using TmIde.ViewModels;

namespace TmIde.ViewModels.Panel;

/// <summary>Un panel del área inferior (Terminal / Output / Debug Console).</summary>

public sealed class OutputPaneViewModel : ViewModelBase
{
    public OutputPaneViewModel(string header, string prompt = "")
    {
        Header = header;
        Prompt = prompt;
    }

    public string Header { get; }

    /// <summary>Prefijo de la línea de entrada (p. ej. <c>&gt;</c> en Terminal). Vacío = sin línea de prompt.</summary>
    public string Prompt { get; }

    public bool HasPrompt => Prompt.Length > 0;

    public ObservableCollection<string> Lines { get; } = new();

    public void AppendLine(string line) => Lines.Add(line);
}
