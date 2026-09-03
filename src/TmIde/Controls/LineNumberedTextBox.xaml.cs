using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace TmIde.Controls;

/// <summary>
/// Editor de código placeholder: <see cref="TextBox"/> monoespaciado con una
/// canaleta de números de línea sincronizada por desplazamiento. Expone
/// <see cref="Text"/>, <see cref="CaretLine"/> y <see cref="CaretColumn"/> como
/// propiedades de dependencia enlazables.
/// </summary>

public partial class LineNumberedTextBox : UserControl
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(LineNumberedTextBox),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

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

    private bool _syncingText;

    public LineNumberedTextBox()
    {
        InitializeComponent();
        Editor.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnEditorScrollChanged));
        UpdateGutter(1);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
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

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (LineNumberedTextBox)d;
        if (control._syncingText)
        {
            return;
        }

        var text = (string)e.NewValue ?? string.Empty;
        control.Editor.Text = text;
        control.UpdateGutter(CountLines(text));
    }

    private void OnEditorTextChanged(object sender, TextChangedEventArgs e)
    {
        _syncingText = true;
        Text = Editor.Text;
        _syncingText = false;

        UpdateGutter(Editor.LineCount > 0 ? Editor.LineCount : CountLines(Editor.Text));
    }

    private void OnEditorSelectionChanged(object sender, RoutedEventArgs e)
    {
        var caretIndex = Editor.CaretIndex;
        var line = Editor.GetLineIndexFromCharacterIndex(caretIndex);
        if (line < 0)
        {
            return;
        }

        var firstChar = Editor.GetCharacterIndexFromLineIndex(line);
        CaretLine = line + 1;
        CaretColumn = caretIndex - firstChar + 1;
    }

    private void OnEditorScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        GutterScroll.ScrollToVerticalOffset(e.VerticalOffset);
    }

    private void UpdateGutter(int lineCount)
    {
        lineCount = Math.Max(lineCount, 1);

        var builder = new StringBuilder(lineCount * 3);
        for (var i = 1; i <= lineCount; i++)
        {
            builder.Append(i);
            if (i < lineCount)
            {
                builder.Append('\n');
            }
        }

        GutterText.Text = builder.ToString();
    }

    private static int CountLines(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 1;
        }

        var count = 1;
        foreach (var c in text)
        {
            if (c == '\n')
            {
                count++;
            }
        }

        return count;
    }
}
