using System;
using System.IO;
using System.Windows;

using Microsoft.Win32;

using TmIde.ViewModels.Start;
using TmIde.Views.Start;

namespace TmIde.Services;

/// <summary>
/// Abre <see cref="StartWindow"/> como diálogo modal sobre la ventana principal
/// y devuelve el proyecto elegido. Los avisos del diálogo se notifican por
/// <c>log</c> (panel inferior).
/// </summary>

public sealed class StartDialogService : IStartDialogService
{
    private readonly Action<string> _log;

    public StartDialogService(Action<string> log)
    {
        _log = log;
    }

    public string? Show()
    {
        var window = new StartWindow
        {
            Owner = Application.Current.MainWindow,
        };

        var viewModel = new StartViewModel(_log, new ProjectScaffolder(), initial => PickFolder(window, initial));
        window.DataContext = viewModel;
        window.ShowDialog();

        return viewModel.ProjectToOpen;
    }

    private static string? PickFolder(Window owner, string initialDirectory)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Seleccionar carpeta",
            InitialDirectory = Directory.Exists(initialDirectory) ? initialDirectory : string.Empty,
        };

        return dialog.ShowDialog(owner) == true ? dialog.FolderName : null;
    }
}
