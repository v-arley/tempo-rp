using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace TmIde.Controls;

/// <summary>
/// Muestra una colección de líneas (Output, salida de la terminal) en un
/// <c>TextEditor</c> de solo lectura. Las altas se añaden al final del documento
/// sin reconstruirlo; cualquier otro cambio (limpiar, quitar, reemplazar) lo
/// reconstruye. Si el usuario estaba al final, sigue la salida; si subió para
/// leer, no le mueve la vista.
/// </summary>

public partial class LogView : UserControl
{
    public static readonly DependencyProperty LinesProperty = DependencyProperty.Register(
        nameof(Lines),
        typeof(IEnumerable<string>),
        typeof(LogView),
        new PropertyMetadata(null, OnLinesChanged));

    public static readonly DependencyProperty SelectionAccentBrushProperty = DependencyProperty.Register(
        nameof(SelectionAccentBrush),
        typeof(Brush),
        typeof(LogView),
        new PropertyMetadata(null, OnSelectionAccentBrushChanged));

    // Margen para considerar "al final" pese al redondeo del desplazamiento.
    private const double BottomTolerance = 2;

    private INotifyCollectionChanged? _observedLines;
    private int _lineCount;

    public LogView()
    {
        InitializeComponent();

        // Solo lectura: el cursor no aporta nada visual.
        Editor.TextArea.Caret.CaretBrush = Brushes.Transparent;
        SetResourceReference(SelectionAccentBrushProperty, "AccentBrush");
    }

    public IEnumerable<string>? Lines
    {
        get => (IEnumerable<string>?)GetValue(LinesProperty);
        set => SetValue(LinesProperty, value);
    }

    /// <summary>Color base de la selección (se aplica con transparencia).</summary>
    public Brush? SelectionAccentBrush
    {
        get => (Brush?)GetValue(SelectionAccentBrushProperty);
        set => SetValue(SelectionAccentBrushProperty, value);
    }

    private static void OnLinesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (LogView)d;

        if (view._observedLines is not null)
        {
            view._observedLines.CollectionChanged -= view.OnLinesCollectionChanged;
            view._observedLines = null;
        }

        if (e.NewValue is INotifyCollectionChanged observable)
        {
            view._observedLines = observable;
            observable.CollectionChanged += view.OnLinesCollectionChanged;
        }

        view.Rebuild();
    }

    private void OnLinesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add
            && e.NewStartingIndex == _lineCount
            && e.NewItems is not null)
        {
            Append(e.NewItems.Cast<string>());
        }
        else
        {
            Rebuild();
        }
    }

    private void Append(IEnumerable<string> newLines)
    {
        var atBottom = IsAtBottom();
        var document = Editor.Document;
        foreach (var line in newLines)
        {
            document.Insert(document.TextLength, _lineCount == 0 ? line : "\n" + line);
            _lineCount++;
        }

        if (atBottom)
        {
            Editor.ScrollToEnd();
        }
    }

    private void Rebuild()
    {
        var lines = Lines?.ToList() ?? [];
        Editor.Text = string.Join('\n', lines);
        _lineCount = lines.Count;
        Editor.ScrollToEnd();
    }

    private bool IsAtBottom() =>
        Editor.VerticalOffset + Editor.ViewportHeight >= Editor.ExtentHeight - BottomTolerance;

    // El acento llega por DynamicResource: al cambiar de tema WPF actualiza la
    // propiedad y aquí se reaplica, porque la selección de AvalonEdit no es enlazable.
    private static void OnSelectionAccentBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is Brush accent)
        {
            EditorThemeBrushes.ApplySelection(((LogView)d).Editor.TextArea, accent);
        }
    }
}
