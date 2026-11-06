using System.Windows;
using TmIde.ViewModels.Help;
using TmIde.Views.Behaviors;

namespace TmIde.Views.Help;

public partial class LanguageReferenceWindow : Window
{
    /// Ejemplo seleccionado para insertar en el editor.
    public string? RequestedExample { get; private set; }

    public LanguageReferenceWindow()
        : this(false)
    {
    }

    public LanguageReferenceWindow(bool canInsert)
    {
        InitializeComponent();

        // Mantener el mismo comportamiento de maximización que TMIDE
        MaximizeToWorkArea.Attach(this);

        DataContext = new LanguageReferenceViewModel();

        // No permitir insertar si no hay un documento activo en el editor
        InsertExampleButton.IsEnabled = canInsert;

        if (!canInsert)
        {
            InsertExampleButton.ToolTip =
                "Abre un archivo en el editor para insertar ejemplos.";
        }
    }

    /// Copia el ejemplo seleccionado al portapapeles.
    private void CopyExample_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is not LanguageReferenceViewModel viewModel)
        {
            return;
        }

        if (viewModel.SelectedSection is null)
        {
            return;
        }

        Clipboard.SetText(viewModel.SelectedSection.Example);
    }

    /// Solicita insertar el ejemplo seleccionado
    private void InsertExample_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is not LanguageReferenceViewModel viewModel)
        {
            return;
        }

        if (viewModel.SelectedSection is null)
        {
            return;
        }

        RequestedExample = viewModel.SelectedSection.Example;

        // Cerrar la ventana para devolver el control al IDE
        DialogResult = true;
    }
}
