
using System.Windows;
using System.Windows.Input;

namespace TmIde.Views.Dialogs;

public partial class NamePromptDialog : Window
{
    public string? EnteredName { get; private set; }

    public NamePromptDialog(
        string title,
        string message,
        string initialName)
    {
        InitializeComponent();

        Title = title;
        DialogTitle.Text = title.ToUpperInvariant();

        PromptLabel.Text = message;
        NameTextBox.Text = initialName;

        Loaded += (_, _) =>
        {
            NameTextBox.Focus();
            NameTextBox.CaretIndex =
                NameTextBox.Text.Length;
        };
    }

    private void Header_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left &&
            e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void Close_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void Cancel_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void Accept_Click(
        object sender,
        RoutedEventArgs e)
    {
        EnteredName = NameTextBox.Text;
        DialogResult = true;
    }
}
