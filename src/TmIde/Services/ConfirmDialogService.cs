
using System.Windows;
using TmIde.Views.Dialogs;

namespace TmIde.Services;

public sealed class ConfirmDialogService : IConfirmDialogService
{
    public bool Confirm(string message)
    {
        var owner = Application.Current?.MainWindow;

        var dialog = new ConfirmDialog(message)
        {
            WindowStartupLocation = owner is null
                ? WindowStartupLocation.CenterScreen
                : WindowStartupLocation.CenterOwner,

            ShowInTaskbar = false
        };

        if (owner is not null)
        {
            dialog.Owner = owner;
        }

        return dialog.ShowDialog() == true;
    }
}
