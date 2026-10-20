using System;
using System.IO;
using System.Windows;

using Microsoft.Win32;

using TmIde.ViewModels.Start;
using TmIde.Views.Start;

namespace TmIde.Services;

/// <summary>
/// Abre <see cref="StartWindow"/> como diálogo modal sobre la ventana principal.
/// Las acciones del diálogo que aún no tienen implementación se notifican por
/// <c>log</c> (panel inferior).
/// </summary>

public sealed class StartDialogService : IStartDialogService
{
    private readonly Action<string> _log;

    public StartDialogService(Action<string> log)
    {
        _log = log;
    }

    public void Show()
    {
        var window = new StartWindow
        {
            Owner = Application.Current.MainWindow,
        };

        window.DataContext = new StartViewModel(_log, new ProjectScaffolder(), initial => PickFolder(window, initial));
        window.ShowDialog();
    }

    private static string? PickFolder(Window owner, string initialDirectory)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Ubicación del proyecto",
            InitialDirectory = Directory.Exists(initialDirectory) ? initialDirectory : string.Empty,
        };

        return dialog.ShowDialog(owner) == true ? dialog.FolderName : null;
    }
}
