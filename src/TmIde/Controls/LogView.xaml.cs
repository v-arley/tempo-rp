using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;


namespace TmIde.Controls;

/// <summary>
/// Muestra una colección de líneas (Output, salida del programa en Run) en un
/// <c>TextEditor</c> de solo lectura. Las altas se añaden al final del documento
/// sin reconstruirlo, igual que el reemplazo de la última línea; cualquier otro
/// cambio (limpiar, quitar, reemplazar otra) lo reconstruye. Si el usuario estaba al final, sigue la salida; si subió para
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

    /// Se dispara al hacer doble clic sobre una línea del log
    public event EventHandler<LogLineDoubleClickedEventArgs>? LineDoubleClicked;

    public LogView()
    {
        InitializeComponent();

        // Ocultar el cursor visual del log de solo lectura
        Editor.TextArea.Caret.CaretBrush = Brushes.Transparent;

        SetResourceReference(SelectionAccentBrushProperty, "AccentBrush");

        // Detectar doble clic sobre el texto de AvalonEdit
        Editor.PreviewMouseLeftButtonDown += OnLogPreviewMouseLeftButtonDown;
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
        else if (e.Action == NotifyCollectionChangedAction.Replace
            && e.NewStartingIndex == _lineCount - 1
            && e.NewItems is [string lastLine])
        {
            ReplaceLastLine(lastLine);
        }
        else
        {
            Rebuild();
        }
    }

    // La salida de un programa llega en trozos que completan la última línea.
    private void ReplaceLastLine(string text)
    {
        var atBottom = IsAtBottom();
        var document = Editor.Document;
        var lastLine = document.GetLineByNumber(document.LineCount);
        document.Replace(lastLine.Offset, lastLine.Length, text);

        if (atBottom)
        {
            Editor.ScrollToEnd();
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

    
    private void OnLogPreviewMouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        // Un clic simple no debe activar la navegación.
        if (e.ClickCount != 2)
        {
            return;
        }

        if (LineDoubleClicked is null)
        {
            return;
        }

        var textView = Editor.TextArea.TextView;

        // Obtener la ubicación real bajo el puntero, teniendo en cuenta el desplazamiento del log
        var position = textView.GetPositionFloor(
            e.GetPosition(textView) + textView.ScrollOffset);

        if (position is null)
        {
            return;
        }

        // AvalonEdit utiliza líneas que comienzan en 1
        var lineNumber = position.Value.Location.Line;

        if (lineNumber < 1 ||
            lineNumber > Editor.Document.LineCount)
        {
            return;
        }

        LineDoubleClicked?.Invoke(
            this,
            new LogLineDoubleClickedEventArgs(lineNumber));
    }

}

/// Información sobre la línea activada en LogView
public sealed class LogLineDoubleClickedEventArgs : EventArgs
{
    public LogLineDoubleClickedEventArgs(int lineNumber)
    {
        LineNumber = lineNumber;
    }

    public int LineNumber { get; }
}
