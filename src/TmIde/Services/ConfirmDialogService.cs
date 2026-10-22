using System.Windows;

namespace TmIde.Services;

/// <summary>Confirmación con el <see cref="MessageBox"/> de WPF.</summary>

public sealed class ConfirmDialogService : IConfirmDialogService
{
    public bool Confirm(string message) =>
        MessageBox.Show(message, "TmIde", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
}
