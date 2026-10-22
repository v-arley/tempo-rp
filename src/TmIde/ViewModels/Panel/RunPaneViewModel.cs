using System;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TmIde.ViewModels.Panel;

/// <summary>
/// Panel Run: salida del programa en ejecución y línea de entrada para
/// <c>input &lt;&lt;</c>, activa solo mientras el programa espera un dato. Solo
/// desde el hilo de la UI.
/// </summary>

public sealed partial class RunPaneViewModel : BottomPaneViewModel
{
    // Como en una consola: la última línea sigue abierta hasta que llega un salto de línea.
    private bool _hasOpenLine;

    private Action<string>? _onInputSubmitted;

    [ObservableProperty]
    private string _currentInput = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private bool _isWaitingForInput;

    public RunPaneViewModel()
        : base("Run")
    {
    }

    /// <summary>Vacía el panel antes de una ejecución.</summary>
    public void Clear()
    {
        Lines.Clear();
        _hasOpenLine = false;
    }

    /// <summary>Texto del programa, tal cual: solo los saltos de línea empiezan línea nueva.</summary>
    public void Write(string text)
    {
        var lines = text.ReplaceLineEndings("\n").Split('\n');

        ContinueOpenLine(lines[0]);
        foreach (var line in lines.Skip(1))
        {
            CloseOpenLine();
            ContinueOpenLine(line);
        }
    }

    /// <summary>Mensaje del IDE (fin, detención, error R###), separado de la salida por una línea en blanco.</summary>
    public void WriteStatus(string message)
    {
        _hasOpenLine = false;
        AppendLine(string.Empty);
        AppendLine(message);
    }

    /// <summary>Activa la línea de entrada; <paramref name="onSubmitted"/> recibe lo que escriba el usuario.</summary>
    public void WaitForInput(Action<string> onSubmitted)
    {
        _onInputSubmitted = onSubmitted;
        IsWaitingForInput = true;
    }

    /// <summary>Desactiva la línea de entrada (dato enviado, o fin de la ejecución).</summary>
    public void StopWaitingForInput()
    {
        _onInputSubmitted = null;
        IsWaitingForInput = false;
    }

    // El dato escrito se muestra como eco, igual que en una consola.
    [RelayCommand(CanExecute = nameof(IsWaitingForInput))]
    private void Submit()
    {
        var input = CurrentInput;
        var onSubmitted = _onInputSubmitted;

        CurrentInput = string.Empty;
        StopWaitingForInput();

        Write(input + "\n");
        onSubmitted?.Invoke(input);
    }

    private void ContinueOpenLine(string text)
    {
        if (text.Length == 0)
        {
            return;
        }

        if (_hasOpenLine)
        {
            Lines[^1] += text;
        }
        else
        {
            Lines.Add(text);
            _hasOpenLine = true;
        }
    }

    // Un salto de línea sin texto pendiente es una línea en blanco.
    private void CloseOpenLine()
    {
        if (!_hasOpenLine)
        {
            Lines.Add(string.Empty);
        }

        _hasOpenLine = false;
    }
}
