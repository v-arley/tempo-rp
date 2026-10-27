
using System.Windows;

using TmIde.ViewModels.Help;

namespace TmIde.Views.Help;

public partial class LanguageReferenceWindow : Window
{
    public LanguageReferenceWindow()
    {
        InitializeComponent();

        DataContext = new LanguageReferenceViewModel();
    }

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
}
