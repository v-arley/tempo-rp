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

public partial class CodeEditor : UserControl
{
    public static readonly DependencyProperty DocumentProperty = DependencyProperty.Register(
        nameof(Document),
        typeof(TextDocument),
        typeof(CodeEditor),
        new FrameworkPropertyMetadata(null, OnDocumentChanged));

    public static readonly DependencyProperty CaretLineProperty = DependencyProperty.Register(
        nameof(CaretLine),
        typeof(int),
        typeof(CodeEditor),
        new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty CaretColumnProperty = DependencyProperty.Register(
        nameof(CaretColumn),
        typeof(int),
        typeof(CodeEditor),
        new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty DiagnosticsProperty = DependencyProperty.Register(
        nameof(Diagnostics),
        typeof(IReadOnlyList<EditorDiagnostic>),
        typeof(CodeEditor),
        new FrameworkPropertyMetadata(null, OnDiagnosticsChanged));

    public static readonly DependencyProperty SelectionAccentBrushProperty = RegisterThemeBrush(nameof(SelectionAccentBrush));

    public static readonly DependencyProperty CaretBrushProperty = RegisterThemeBrush(nameof(CaretBrush));

    public static readonly DependencyProperty FoldingMarkerBrushProperty = RegisterThemeBrush(nameof(FoldingMarkerBrush));

    public static readonly DependencyProperty SelectedFoldingMarkerBrushProperty = RegisterThemeBrush(nameof(SelectedFoldingMarkerBrush));

    public static readonly DependencyProperty FoldingMarkerBackgroundBrushProperty = RegisterThemeBrush(nameof(FoldingMarkerBackgroundBrush));

    private const double SearchMarkerOpacity = 0.4;

    private readonly TmSyntaxColorizer _syntaxColorizer = new();
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
    private readonly TmIndentationStrategy _indentationStrategy = new();
    private FoldingManager? _foldingManager;

    public CodeEditor()
    {
        InitializeComponent();

        var textView = Editor.TextArea.TextView;
        textView.LineTransformers.Add(_syntaxColorizer);
        _diagnosticsRenderer = new DiagnosticsRenderer(textView);
        textView.BackgroundRenderers.Add(_diagnosticsRenderer);
        textView.MouseHover += OnTextViewMouseHover;
        textView.MouseHoverStopped += OnTextViewMouseHoverStopped;
        _diagnosticToolTip.ContentTemplate = (DataTemplate)Resources["DiagnosticToolTipTemplate"];
        _diagnosticToolTip.SetResourceReference(BackgroundProperty, "SurfaceBackgroundBrush");
        _diagnosticToolTip.SetResourceReference(ForegroundProperty, "PrimaryForegroundBrush");
        _diagnosticToolTip.SetResourceReference(Control.BorderBrushProperty, "DividerBrush");
        Editor.TextChanged += OnEditorTextChanged;
        _searchPanel = SearchPanel.Install(Editor);

        // Sangría de 4 espacios (Tab inserta espacios, no tabulaciones); Enter la
        // hereda y '<' al inicio de línea cierra el bloque.
        Editor.Options.IndentationSize = TmIndentationStrategy.IndentSize;
        Editor.Options.ConvertTabsToSpaces = true;
        Editor.TextArea.IndentationStrategy = _indentationStrategy;
        Editor.TextArea.TextEntered += OnTextEntered;
        _foldingTimer.Tick += OnFoldingTimerTick;
        InstallFolding();
        Editor.TextArea.Caret.PositionChanged += OnCaretPositionChanged;

        SetResourceReference(SelectionAccentBrushProperty, "AccentBrush");
        SetResourceReference(CaretBrushProperty, "PrimaryForegroundBrush");
        SetResourceReference(FoldingMarkerBrushProperty, "TertiaryForegroundBrush");
        SetResourceReference(SelectedFoldingMarkerBrushProperty, "SecondaryForegroundBrush");
        SetResourceReference(FoldingMarkerBackgroundBrushProperty, "SurfaceBackgroundBrush");
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

    /// <summary>Color base de la selección y de las coincidencias de búsqueda (se aplica con transparencia).</summary>
    public Brush? SelectionAccentBrush
    {
        get => (Brush?)GetValue(SelectionAccentBrushProperty);
        set => SetValue(SelectionAccentBrushProperty, value);
    }

    public Brush? CaretBrush
    {
        get => (Brush?)GetValue(CaretBrushProperty);
        set => SetValue(CaretBrushProperty, value);
    }

    /// <summary>Marcadores del margen de plegado y texto de un bloque plegado.</summary>
    public Brush? FoldingMarkerBrush
    {
        get => (Brush?)GetValue(FoldingMarkerBrushProperty);
        set => SetValue(FoldingMarkerBrushProperty, value);
    }

    public Brush? SelectedFoldingMarkerBrush
    {
        get => (Brush?)GetValue(SelectedFoldingMarkerBrushProperty);
        set => SetValue(SelectedFoldingMarkerBrushProperty, value);
    }

    public Brush? FoldingMarkerBackgroundBrush
    {
        get => (Brush?)GetValue(FoldingMarkerBackgroundBrushProperty);
        set => SetValue(FoldingMarkerBackgroundBrushProperty, value);
    }

    private static DependencyProperty RegisterThemeBrush(string name) => DependencyProperty.Register(
        name,
        typeof(Brush),
        typeof(CodeEditor),
        new FrameworkPropertyMetadata(null, OnThemeBrushChanged));

    private static void OnDiagnosticsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((CodeEditor)d).ApplyDiagnostics();

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

    // El aspecto del tooltip está en DiagnosticToolTipTemplate (XAML); aquí solo
    // se eligen los diagnósticos que hay bajo el puntero.
    private void OnTextViewMouseHover(object? sender, MouseEventArgs e)
    {
        var textView = Editor.TextArea.TextView;
        var position = textView.GetPositionFloor(e.GetPosition(textView) + textView.ScrollOffset);
        if (position is null)
        {
            return;
        }

        var offset = Editor.Document.GetOffset(position.Value.Location);
        var diagnosticsUnderPointer = _diagnosticsRenderer.SegmentsAt(offset)
            .Select(segment => segment.Diagnostic)
            .ToList();

        if (diagnosticsUnderPointer.Count == 0)
        {
            return;
        }

        _diagnosticToolTip.Content = diagnosticsUnderPointer;
        _diagnosticToolTip.IsOpen = true;
    }

    private void OnTextViewMouseHoverStopped(object? sender, MouseEventArgs e) => _diagnosticToolTip.IsOpen = false;

    private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((CodeEditor)d).SwitchDocument((TextDocument?)e.OldValue, (TextDocument?)e.NewValue);

    private void SwitchDocument(TextDocument? previous, TextDocument? next)
    {
        // El estado se lee antes del cambio: en ese momento el editor aún muestra el anterior.
        if (previous is not null)
        {
            var foldedStarts = _foldingManager?.AllFoldings
                .Where(folding => folding.IsFolded)
                .Select(folding => folding.StartOffset)
                .ToList() ?? [];
            _viewStates.AddOrUpdate(previous, new ViewState(
                Editor.TextArea.Caret.Offset, Editor.VerticalOffset, Editor.HorizontalOffset, foldedStarts));
        }

        _diagnosticToolTip.IsOpen = false;
        Editor.Document = next ?? new TextDocument();
        Editor.IsReadOnly = next is null;
        _syntaxColorizer.Rescan(Editor.Document);

        // El gestor de plegado está ligado a un documento: se recrea con el nuevo.
        InstallFolding();

        // El orden en que se actualizan los enlaces Document y Diagnostics no está
        // garantizado: se reaplican los diagnósticos contra el documento ya cambiado.
        ApplyDiagnostics();

        if (next is not null && _viewStates.TryGetValue(next, out var state))
        {
            Editor.TextArea.Caret.Offset = Math.Min(state.CaretOffset, next.TextLength);

            if (_foldingManager is not null)
            {
                var previouslyFolded = _foldingManager.AllFoldings
                    .Where(folding => state.FoldedStarts.Contains(folding.StartOffset));
                foreach (var folding in previouslyFolded)
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
            _indentationStrategy.IndentLine(Editor.Document, line);
        }
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        _syntaxColorizer.Rescan(Editor.Document);
        _foldingTimer.Stop();
        _foldingTimer.Start();
    }

    private void InstallFolding()
    {
        _foldingTimer.Stop();
        if (_foldingManager is not null)
        {
            FoldingManager.Uninstall(_foldingManager);
        }

        _foldingManager = FoldingManager.Install(Editor.TextArea);
        TmFoldingStrategy.UpdateFoldings(_foldingManager, Editor.Document);
        ApplyThemeBrushes();
    }

    private void OnFoldingTimerTick(object? sender, EventArgs e)
    {
        _foldingTimer.Stop();
        if (_foldingManager is not null)
        {
            TmFoldingStrategy.UpdateFoldings(_foldingManager, Editor.Document);
        }
    }

    private void OnCaretPositionChanged(object? sender, EventArgs e)
    {
        CaretLine = Editor.TextArea.Caret.Line;
        CaretColumn = Editor.TextArea.Caret.Column;
    }

    // Los pinceles de tema llegan por DynamicResource: al cambiar de tema WPF
    // actualiza estas propiedades y aquí se reaplican, porque en AvalonEdit no son
    // enlazables. El redibujado recoge además los colores de sintaxis y de
    // diagnósticos, que se resuelven en cada pintado.
    private static void OnThemeBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (CodeEditor)d;
        editor.ApplyThemeBrushes();
        editor.Editor.TextArea.TextView.Redraw();
    }

    private void ApplyThemeBrushes()
    {
        // Mientras el control no está en el árbol visual los recursos aún no se
        // han resuelto (pinceles nulos): se aplica lo que haya disponible.
        if (SelectionAccentBrush is { } accent)
        {
            EditorThemeBrushes.ApplySelection(Editor.TextArea, accent);
            _searchPanel.MarkerBrush = EditorThemeBrushes.WithOpacity(accent, SearchMarkerOpacity);
        }

        if (CaretBrush is { } caret)
        {
            Editor.TextArea.Caret.CaretBrush = caret;
        }

        // Margen de plegado y texto de un bloque plegado.
        if (FoldingMarkerBrush is { } marker
            && SelectedFoldingMarkerBrush is { } selectedMarker
            && FoldingMarkerBackgroundBrush is { } markerBackground)
        {
            FoldingElementGenerator.TextBrush = marker;
            foreach (var margin in Editor.TextArea.LeftMargins.OfType<FoldingMargin>())
            {
                margin.FoldingMarkerBrush = marker;
                margin.FoldingMarkerBackgroundBrush = markerBackground;
                margin.SelectedFoldingMarkerBrush = selectedMarker;
                margin.SelectedFoldingMarkerBackgroundBrush = markerBackground;
            }
        }
    }
}
