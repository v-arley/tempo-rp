using System;
using System.Windows.Input;

using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Search;

using TmIde.Services.Search;

namespace TmIde.Controls.Search;

/// <summary>
/// Buscar (Ctrl+F) y reemplazar (Ctrl+H) en el documento que muestra el
/// editor. La búsqueda y el resaltado son los del <see cref="SearchPanel"/> de
/// AvalonEdit; este controlador añade el reemplazo leyendo las mismas opciones
/// del panel (mayúsculas, palabra completa, expresión regular), de modo que
/// siempre se reemplaza exactamente lo que está resaltado.
/// </summary>
public sealed class FindReplaceController
{
    private const string NoMatchesStatus = "Sin coincidencias";

    private readonly SearchPanel _panel;
    private readonly TextArea _textArea;
    private readonly Func<bool> _isReadOnly;

    /// <param name="isReadOnly">Verdadero cuando no hay documento editable; entonces solo se busca.</param>
    public FindReplaceController(SearchPanel panel, TextArea textArea, Func<bool> isReadOnly)
    {
        _panel = panel;
        _textArea = textArea;
        _isReadOnly = isReadOnly;

        _panel.CommandBindings.Add(new CommandBinding(SearchPanelReplace.ReplaceNext, (_, _) => ReplaceNext(), OnCanExecuteReplace));
        _panel.CommandBindings.Add(new CommandBinding(SearchPanelReplace.ReplaceAll, (_, _) => ReplaceAll(), OnCanExecuteReplace));
        _panel.SearchOptionsChanged += (_, _) => SearchPanelReplace.SetStatus(_panel, string.Empty);
    }

    /// <summary>Abre el panel solo con la búsqueda.</summary>
    public void OpenFind() => Open(showReplace: false);

    /// <summary>Abre el panel con la fila de reemplazo visible.</summary>
    public void OpenReplace() => Open(showReplace: !_isReadOnly());

    private void Open(bool showReplace)
    {
        // Como en el Ctrl+F de AvalonEdit: una selección de una sola línea se
        // convierte en el texto a buscar.
        var selected = _textArea.Selection.GetText();
        if (!string.IsNullOrEmpty(selected) && !selected.Contains('\n', StringComparison.Ordinal))
        {
            _panel.SearchPattern = selected;
        }

        SearchPanelReplace.SetIsReplaceVisible(_panel, showReplace);
        SearchPanelReplace.SetStatus(_panel, string.Empty);
        _panel.Open();
        _panel.Reactivate();
    }

    private void OnCanExecuteReplace(object sender, CanExecuteRoutedEventArgs e) =>
        e.CanExecute = !_isReadOnly() && !string.IsNullOrEmpty(_panel.SearchPattern);

    private void ReplaceNext()
    {
        if (!TryCompileQuery(out var searcher))
        {
            return;
        }

        var selection = new TextMatch(_textArea.Selection.SurroundingSegment?.Offset ?? _textArea.Caret.Offset, _textArea.Selection.Length);
        var step = DocumentReplacer.ReplaceNext(_textArea.Document, searcher, SearchPanelReplace.GetReplacementText(_panel), selection);

        SearchPanelReplace.SetStatus(_panel, step.Next is null && !step.Replaced ? NoMatchesStatus : string.Empty);
        if (step.Next is { } next)
        {
            SelectMatch(next);
        }
    }

    private void ReplaceAll()
    {
        if (!TryCompileQuery(out var searcher))
        {
            return;
        }

        var count = DocumentReplacer.ReplaceAll(_textArea.Document, searcher, SearchPanelReplace.GetReplacementText(_panel));
        SearchPanelReplace.SetStatus(_panel, ReplacementSummary(count));
    }

    private static string ReplacementSummary(int count) => count switch
    {
        0 => NoMatchesStatus,
        1 => "1 reemplazo",
        _ => $"{count} reemplazos",
    };

    private bool TryCompileQuery(out TextSearcher searcher)
    {
        var query = new TextSearchQuery(_panel.SearchPattern, _panel.MatchCase, _panel.WholeWords, _panel.UseRegex);
        var compiled = query.TryCompile(out searcher, out var error);

        if (!compiled)
        {
            SearchPanelReplace.SetStatus(_panel, error);
        }

        return compiled;
    }

    private void SelectMatch(TextMatch match)
    {
        _textArea.Selection = Selection.Create(_textArea, match.Offset, match.End);
        _textArea.Caret.Offset = match.End;
        _textArea.Caret.BringCaretToView();
    }
}
