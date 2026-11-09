using System;
using System.Windows.Media;

using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

using TmIde.Services.Completion;

namespace TmIde.Controls.Completion;

/// <summary>Explicación de la sugerencia seleccionada; la ficha al pie de la lista la pinta con su DataTemplate.</summary>
public sealed record CompletionDescription(string Title, string Detail, string Text);

/// <summary>
/// Adapta una <see cref="CompletionSuggestion"/> al contrato de AvalonEdit
/// (Adapter). La fila y la explicación se dibujan con los DataTemplate de
/// <c>Styles.xaml</c>, que reciben la sugerencia y su descripción como datos.
/// </summary>
public sealed class TempoCompletionData : ICompletionData
{
    public TempoCompletionData(CompletionSuggestion suggestion)
    {
        Suggestion = suggestion;
        Details = new CompletionDescription(suggestion.Label, suggestion.Detail, suggestion.Description);
    }

    public CompletionSuggestion Suggestion { get; }

    /// <summary>Explicación que muestra la ficha al pie de la lista (plantilla de CompletionList en Styles.xaml).</summary>
    public CompletionDescription Details { get; }

    public string Text => Suggestion.Label;

    public object Content => Suggestion;

    /// <summary>
    /// Siempre nulo: con una descripción, AvalonEdit abre su propio tooltip de
    /// sistema. La explicación va en <see cref="Details"/>, dentro del contenedor.
    /// </summary>
    public object? Description => null;

    public ImageSource? Image => null;

    public double Priority => 0;

    /// <summary>Sustituye lo escrito por el texto de la sugerencia, con la sangría de la línea, y coloca el cursor.</summary>
    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        var document = textArea.Document;
        var line = document.GetLineByOffset(completionSegment.Offset);
        var indentation = document.GetText(TextUtilities.GetLeadingWhitespace(document, line));

        var insertion = Suggestion.ExpandFor(indentation);
        document.Replace(completionSegment, insertion.Text);
        textArea.Caret.Offset = completionSegment.Offset + insertion.CaretIndex;
    }
}
