
using System.Windows;
using TmIde.Views.Dialogs;

namespace TmIde.Services;

public sealed class NamePromptService : INamePromptService
{
    public string? RequestName(
        string title,
        string message,
        string initialName = "")
    {
        var owner = Application.Current?.MainWindow;

        var dialog = new NamePromptDialog(
            title,
            message,
            initialName)
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

        return dialog.ShowDialog() == true
            ? dialog.EnteredName
            : null;
    }
}
