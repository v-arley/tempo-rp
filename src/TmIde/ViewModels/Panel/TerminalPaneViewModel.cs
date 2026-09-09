using System;
using System.IO;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using TmIde.Services.Terminal;

namespace TmIde.ViewModels.Panel;

/// <summary>
/// Panel Terminal: un shell mínimo sobre el sistema de archivos real (cd, ls,
/// mkdir, rm, mv, cp, cat, echo, clear, help). La entrada se envía con Enter y el
/// historial se recorre con las flechas ↑ / ↓.
/// </summary>

public sealed partial class TerminalPaneViewModel : BottomPaneViewModel
{
    private readonly TerminalShell _shell;

    [ObservableProperty]
    private string _currentInput = string.Empty;

    [ObservableProperty]
    private string _prompt = string.Empty;

    private int _historyCursor;

    public TerminalPaneViewModel()
        : base("Terminal")
    {
        _shell = new TerminalShell();
        _historyCursor = 0;

        Prompt = BuildPrompt();
        AppendLine("Terminal TM — escribe 'help' para ver los comandos disponibles.");
    }

    [RelayCommand]
    private void Submit()
    {
        var line = CurrentInput;
        AppendLine($"{Prompt} {line}");
        CurrentInput = string.Empty;

        if (!string.IsNullOrWhiteSpace(line))
        {
            _shell.Execute(line, AppendLine, AppendLine, Lines.Clear);
        }

        _historyCursor = _shell.History.Count;
        Prompt = BuildPrompt();
    }

    [RelayCommand]
    private void HistoryPrevious()
    {
        var history = _shell.History;
        if (history.Count == 0)
        {
            return;
        }

        _historyCursor = Math.Max(0, _historyCursor - 1);
        CurrentInput = history[_historyCursor];
    }

    [RelayCommand]
    private void HistoryNext()
    {
        var history = _shell.History;
        if (_historyCursor >= history.Count - 1)
        {
            _historyCursor = history.Count;
            CurrentInput = string.Empty;
            return;
        }

        _historyCursor++;
        CurrentInput = history[_historyCursor];
    }

    private string BuildPrompt()
    {
        var directory = _shell.WorkingDirectory;
        var leaf = Path.GetFileName(directory);

        return string.IsNullOrEmpty(leaf) ? $"{directory} >" : $"{leaf} >";
    }
}
