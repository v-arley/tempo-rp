using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using TmIde.ViewModels.Explorer;

namespace TmIde.Views.Explorer;

public partial class FileExplorerView : UserControl
{
    public FileExplorerView()
    {
        InitializeComponent();
    }

    private void OnTreeSelectedItemChanged(
        object sender,
        RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is FileExplorerViewModel viewModel)
        {
            viewModel.SelectedNode =
                e.NewValue as FileNodeViewModel;
        }
    }

    private void OnTreePreviewMouseRightButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        DependencyObject? current =
            e.OriginalSource as DependencyObject;

        while (current is not null &&
               current is not TreeViewItem)
        {
            current = current is Visual
                ? VisualTreeHelper.GetParent(current)
                : LogicalTreeHelper.GetParent(current);
        }

        if (current is TreeViewItem item)
        {
            item.IsSelected = true;
            item.Focus();
        }
    }

    private void OnTreePreviewKeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (DataContext is not FileExplorerViewModel viewModel)
        {
            return;
        }

        if (e.Key == Key.F2)
        {
            if (viewModel.RenameSelectedCommand.CanExecute(null))
            {
                viewModel.RenameSelectedCommand.Execute(null);
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Delete)
        {
            if (viewModel.DeleteSelectedCommand.CanExecute(null))
            {
                viewModel.DeleteSelectedCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}