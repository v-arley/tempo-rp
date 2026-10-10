using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Linq;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;
using TmIde.Controls.Diagnostics;
using TmIde.Controls.Syntax;
using TmIde.Services;
using TmIde.Services.Analysis;

namespace TmIde.Controls;

/// <summary>
/// Editor de código TM sobre AvalonEdit con coloreado de sintaxis y subrayado de
/// diagnósticos. Edita el <see cref="Document"/> que se le enlace (uno por
/// pestaña) y recuerda cursor, desplazamiento y plegados de cada documento al
/// cambiar de uno a otro. Incluye búsqueda (Ctrl+F), plegado de bloques y
/// sangría automática. Expone <see cref="CaretLine"/> y <see cref="CaretColumn"/> como
/// propiedades de dependencia enlazables.
/// </summary>

public partial class LineNumberedTextBox : UserControl
{
    public static readonly DependencyProperty DocumentProperty = DependencyProperty.Register(
        nameof(Document),
        typeof(TextDocument),
        typeof(LineNumberedTextBox),
        new FrameworkPropertyMetadata(null, OnDocumentChanged));

    public static readonly DependencyProperty CaretLineProperty = DependencyProperty.Register(
        nameof(CaretLine),
        typeof(int),
        typeof(LineNumberedTextBox),
        new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty CaretColumnProperty = DependencyProperty.Register(
        nameof(CaretColumn),
        typeof(int),
        typeof(LineNumberedTextBox),
        new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty DiagnosticsProperty = DependencyProperty.Register(
        nameof(Diagnostics),
        typeof(IReadOnlyList<EditorDiagnostic>),
        typeof(LineNumberedTextBox),
        new FrameworkPropertyMetadata(null, OnDiagnosticsChanged));

    private readonly TmSyntaxColorizer _colorizer = new();
    private readonly DiagnosticsRenderer _diagnosticsRenderer;
    private readonly ToolTip _diagnosticToolTip = new() { Placement = PlacementMode.Mouse, StaysOpen = true };

    // Cursor y desplazamiento de cada documento mientras no es el visible. La
    // tabla es débil: al cerrar una pestaña y soltarse su documento, el estado se va con él.
    private readonly ConditionalWeakTable<TextDocument, ViewState> _viewStates = new();

    private sealed record ViewState(
        int CaretOffset,
        double VerticalOffset,
        double HorizontalOffset,
        IReadOnlyCollection<int> FoldedStarts);

    // Los plegados se recalculan tras una pausa al escribir, no en cada pulsación.
    private readonly DispatcherTimer _foldingTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };
    private readonly SearchPanel _searchPanel;
    private readonly TmIndentationStrategy _indentation = new();
    private FoldingManager? _folding;

    public LineNumberedTextBox()
    {
        InitializeComponent();

        var textView = Editor.TextArea.TextView;
        textView.LineTransformers.Add(_colorizer);
        _diagnosticsRenderer = new DiagnosticsRenderer(textView);
        textView.BackgroundRenderers.Add(_diagnosticsRenderer);
        textView.MouseHover += OnTextViewMouseHover;
        textView.MouseHoverStopped += OnTextViewMouseHoverStopped;
        _diagnosticToolTip.SetResourceReference(BackgroundProperty, "SurfaceBackgroundBrush");
        _diagnosticToolTip.SetResourceReference(ForegroundProperty, "PrimaryForegroundBrush");
        _diagnosticToolTip.SetResourceReference(Control.BorderBrushProperty, "DividerBrush");
        Editor.TextChanged += OnEditorTextChanged;
        _searchPanel = SearchPanel.Install(Editor);

        // Sangría de 4 espacios (Tab inserta espacios, no tabulaciones); Enter la
        // hereda y '<' al inicio de línea cierra el bloque.
        Editor.Options.IndentationSize = TmIndentationStrategy.IndentSize;
        Editor.Options.ConvertTabsToSpaces = true;
        Editor.TextArea.IndentationStrategy = _indentation;
        Editor.TextArea.TextEntered += OnTextEntered;
        _foldingTimer.Tick += OnFoldingTimerTick;
        InstallFolding();
        Editor.TextArea.Caret.PositionChanged += OnCaretPositionChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;

        ApplyThemeBrushes();
    }

    /// <summary>Documento que se edita; si es nulo el editor queda vacío y de solo lectura.</summary>
    public TextDocument? Document
    {
        get => (TextDocument?)GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    public int CaretLine
    {
        get => (int)GetValue(CaretLineProperty);
        set => SetValue(CaretLineProperty, value);
    }

    public int CaretColumn
    {
        get => (int)GetValue(CaretColumnProperty);
        set => SetValue(CaretColumnProperty, value);
    }

    /// <summary>Diagnósticos del documento actual; el control los subraya y muestra su detalle al pasar el mouse.</summary>
    public IReadOnlyList<EditorDiagnostic>? Diagnostics
    {
        get => (IReadOnlyList<EditorDiagnostic>?)GetValue(DiagnosticsProperty);
        set => SetValue(DiagnosticsProperty, value);
    }

    private static void OnDiagnosticsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((LineNumberedTextBox)d).ApplyDiagnostics();

    private void ApplyDiagnostics()
    {
        _diagnosticToolTip.IsOpen = false;
        if (Diagnostics is { Count: > 0 } diagnostics)
        {
            _diagnosticsRenderer.Update(diagnostics, Editor.Document);
        }
        else
        {
            _diagnosticsRenderer.Clear();
        }
    }

    private void OnTextViewMouseHover(object? sender, MouseEventArgs e)
    {
        var textView = Editor.TextArea.TextView;
        var position = textView.GetPositionFloor(e.GetPosition(textView) + textView.ScrollOffset);
        if (position is null)
        {
            return;
        }

        var offset = Editor.Document.GetOffset(position.Value.Location);
        var found = false;
        var panel = new StackPanel { MaxWidth = 420 };
        foreach (var segment in _diagnosticsRenderer.SegmentsAt(offset))
        {
            found = true;
            var d = segment.Diagnostic;
            var key = d.Severity == EditorDiagnosticSeverity.Warning ? "DiagnosticWarningBrush" : "DiagnosticErrorBrush";
            var header = new TextBlock { FontWeight = FontWeights.SemiBold, Text = $"{d.Code}  {d.Title}" };
            header.SetResourceReference(TextBlock.ForegroundProperty, key);
            panel.Children.Add(header);
            panel.Children.Add(new TextBlock { Text = d.Description, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) });
            if (d.Suggestion.Length > 0)
            {
                var hint = new TextBlock { Text = d.Suggestion, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0), Opacity = 0.75 };
                panel.Children.Add(hint);
            }
        }

        if (!found)
        {
            return;
        }

        _diagnosticToolTip.Content = panel;
        _diagnosticToolTip.IsOpen = true;
    }

    private void OnTextViewMouseHoverStopped(object? sender, MouseEventArgs e) => _diagnosticToolTip.IsOpen = false;

    private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((LineNumberedTextBox)d).SwitchDocument((TextDocument?)e.OldValue, (TextDocument?)e.NewValue);

    private void SwitchDocument(TextDocument? previous, TextDocument? next)
    {
        // El estado se lee antes del cambio: en ese momento el editor aún muestra el anterior.
        if (previous is not null)
        {
            var folded = _folding?.AllFoldings.Where(f => f.IsFolded).Select(f => f.StartOffset).ToList() ?? [];
            _viewStates.AddOrUpdate(previous, new ViewState(
                Editor.TextArea.Caret.Offset, Editor.VerticalOffset, Editor.HorizontalOffset, folded));
        }

        _diagnosticToolTip.IsOpen = false;
        Editor.Document = next ?? new TextDocument();
        Editor.IsReadOnly = next is null;
        _colorizer.Rescan(Editor.Document);

        // El gestor de plegado está ligado a un documento: se recrea con el nuevo.
        InstallFolding();

        // El orden en que se actualizan los enlaces Document y Diagnostics no está
        // garantizado: se reaplican los diagnósticos contra el documento ya cambiado.
        ApplyDiagnostics();

        if (next is not null && _viewStates.TryGetValue(next, out var state))
        {
            Editor.TextArea.Caret.Offset = Math.Min(state.CaretOffset, next.TextLength);

            if (_folding is not null)
            {
                foreach (var folding in _folding.AllFoldings.Where(f => state.FoldedStarts.Contains(f.StartOffset)))
                {
                    folding.IsFolded = true;
                }
            }

            // El desplazamiento solo se puede fijar cuando el nuevo documento ya tiene diseño.
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                Editor.ScrollToVerticalOffset(state.VerticalOffset);
                Editor.ScrollToHorizontalOffset(state.HorizontalOffset);
            }));
        }
    }

    // Al escribir '<' como primer carácter de la línea se realinea con el '>' que abrió el bloque.
    private void OnTextEntered(object? sender, TextCompositionEventArgs e)
    {
        if (e.Text != "<")
        {
            return;
        }

        var line = Editor.Document.GetLineByOffset(Editor.TextArea.Caret.Offset);
        if (Editor.Document.GetText(line).Trim() == "<")
        {
            _indentation.IndentLine(Editor.Document, line);
        }
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        _colorizer.Rescan(Editor.Document);
        _foldingTimer.Stop();
        _foldingTimer.Start();
    }

    private void InstallFolding()
    {
        _foldingTimer.Stop();
        if (_folding is not null)
        {
            FoldingManager.Uninstall(_folding);
        }

        _folding = FoldingManager.Install(Editor.TextArea);
        TmFoldingStrategy.UpdateFoldings(_folding, Editor.Document);
        ApplyThemeBrushes();
    }

    private void OnFoldingTimerTick(object? sender, EventArgs e)
    {
        _foldingTimer.Stop();
        if (_folding is not null)
        {
            TmFoldingStrategy.UpdateFoldings(_folding, Editor.Document);
        }
    }

    private void OnCaretPositionChanged(object? sender, EventArgs e)
    {
        CaretLine = Editor.TextArea.Caret.Line;
        CaretColumn = Editor.TextArea.Caret.Column;
    }

    // Selección y cursor de AvalonEdit no son propiedades de dependencia: se
    // vuelven a aplicar cuando el diccionario de tema se reemplaza.
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ThemeService.ThemeApplied += OnThemeApplied;
        ApplyThemeBrushes();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ThemeService.ThemeApplied -= OnThemeApplied;
    }

    private void OnThemeApplied(object? sender, EventArgs e)
    {
        ApplyThemeBrushes();
        Editor.TextArea.TextView.Redraw();
    }

    private void ApplyThemeBrushes()
    {
        EditorThemeBrushes.Apply(Editor);

        if (TryFindResource("AccentBrush") is Brush accent)
        {
            var marker = accent.CloneCurrentValue();
            marker.Opacity = 0.4;
            _searchPanel.MarkerBrush = marker;
        }

        // Margen de plegado y texto de un bloque plegado, con los colores del tema.
        if (TryFindResource("TertiaryForegroundBrush") is Brush muted
            && TryFindResource("SecondaryForegroundBrush") is Brush secondary
            && TryFindResource("SurfaceBackgroundBrush") is Brush surface)
        {
            FoldingElementGenerator.TextBrush = muted;
            foreach (var margin in Editor.TextArea.LeftMargins.OfType<FoldingMargin>())
            {
                margin.FoldingMarkerBrush = muted;
                margin.FoldingMarkerBackgroundBrush = surface;
                margin.SelectedFoldingMarkerBrush = secondary;
                margin.SelectedFoldingMarkerBackgroundBrush = surface;
            }
        }
    }
}
