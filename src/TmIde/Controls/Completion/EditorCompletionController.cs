using System;
using System.Windows;
using System.Windows.Input;

using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Editing;

using Tempo.Language.Syntax;

using TmIde.Services.Completion;

namespace TmIde.Controls.Completion;

/// <summary>
/// Decide cuándo se abre y se cierra la lista de autocompletado sobre un
/// <see cref="TextArea"/>. Se abre sola al escribir
/// <see cref="AutomaticPrefixLength"/> letras de una palabra, o a petición con
/// <see cref="ShowSuggestions"/> (Ctrl+Espacio). Al escribir un carácter que no
/// forma parte de un identificador se cierra sin insertar nada: aceptar una
/// sugerencia es siempre explícito (Enter, Tab o doble clic).
/// </summary>
public sealed class EditorCompletionController
{
    /// <summary>Letras que hay que escribir para que la lista aparezca sola.</summary>
    public const int AutomaticPrefixLength = 2;

    private const double WindowWidth = 372;
    private const double WindowMaxHeight = 420;

    private readonly TextArea _textArea;
    private readonly CompletionEngine _engine;
    private CompletionWindow? _window;

    public EditorCompletionController(TextArea textArea, CompletionEngine engine)
    {
        _textArea = textArea;
        _engine = engine;

        _textArea.TextEntering += OnTextEntering;
        _textArea.TextEntered += OnTextEntered;
    }

    public bool IsOpen => _window is not null;

    /// <summary>Abre la lista en el cursor aunque no se haya escrito nada. No hace nada en comentarios o cadenas.</summary>
    public void ShowSuggestions() => Open(requiredPrefixLength: 0);

    public void Close() => _window?.Close();

    private void OnTextEntering(object? sender, TextCompositionEventArgs e)
    {
        if (_window is not null && e.Text.Length > 0 && !SyntaxFacts.IsIdentifierPart(e.Text[0]))
        {
            _window.Close();
        }
    }

    private void OnTextEntered(object? sender, TextCompositionEventArgs e)
    {
        if (_window is null && e.Text.Length > 0 && SyntaxFacts.IsIdentifierPart(e.Text[0]))
        {
            Open(AutomaticPrefixLength);
        }
    }

    private void Open(int requiredPrefixLength)
    {
        if (_window is not null || _textArea.Document is null)
        {
            return;
        }

        var context = CompletionContext.At(_textArea.Document.Text, _textArea.Caret.Offset);
        if (context.Prefix.Length < requiredPrefixLength)
        {
            return;
        }

        var suggestions = _engine.SuggestionsFor(context);
        if (suggestions.Count == 0)
        {
            return;
        }

        // Ventana sin marco del sistema y transparente: el contenedor de Styles.xaml
        // dibuja el marco recto y la sombra. El ancho incluye el margen de la sombra.
        var window = new CompletionWindow(_textArea)
        {
            StartOffset = context.PrefixStart,
            CloseWhenCaretAtBeginning = requiredPrefixLength > 0,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Width = WindowWidth,
            MaxHeight = WindowMaxHeight,
        };

        foreach (var suggestion in suggestions)
        {
            window.CompletionList.CompletionData.Add(new TempoCompletionData(suggestion));
        }

        window.Closed += OnWindowClosed;
        _window = window;
        window.Show();

        // Siempre hay una fila seleccionada: Enter tiene qué aceptar y la ficha al pie qué explicar.
        if (context.Prefix.Length > 0)
        {
            window.CompletionList.SelectItem(context.Prefix);
        }
        else
        {
            window.CompletionList.SelectedItem = window.CompletionList.CompletionData[0];
        }
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        if (sender is CompletionWindow window)
        {
            window.Closed -= OnWindowClosed;
        }

        _window = null;
    }
}
