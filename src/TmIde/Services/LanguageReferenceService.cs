
using System.Windows;

using TmIde.Views.Help;

namespace TmIde.Services;

public sealed class LanguageReferenceService
    : ILanguageReferenceService
{
    public void Show()
    {
        var dialog = new LanguageReferenceWindow();

        var owner = Application.Current?.MainWindow;

        if (owner is not null && owner.IsVisible)
        {
            dialog.Owner = owner;
            dialog.WindowStartupLocation =
                WindowStartupLocation.CenterOwner;
        }
        else
        {
            dialog.WindowStartupLocation =
                WindowStartupLocation.CenterScreen;
        }

        dialog.ShowDialog();
    }
}
