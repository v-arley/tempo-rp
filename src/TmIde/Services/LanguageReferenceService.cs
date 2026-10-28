using System.Windows;
using TmIde.ViewModels.Editor;
using TmIde.Views.Help;

namespace TmIde.Services;

public sealed class LanguageReferenceService
    : ILanguageReferenceService
{
    private readonly EditorHostViewModel _editorHost;

    public LanguageReferenceService(
        EditorHostViewModel editorHost)
    {
        _editorHost = editorHost;
    }

    public void Show()
    {
        // Consultar si existe un documento abierto
        var hasActiveDocument =
            _editorHost.SelectedDocument is not null;

        var dialog = new LanguageReferenceWindow(
            hasActiveDocument);

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

        // Mostrar la ventana de referencia
        var result = dialog.ShowDialog();

        // Solo insertar si el usuario presiono expresamente "Insertar en el editor"
        if (result == true &&
            dialog.RequestedExample is { } example)
        {
            _editorHost.InsertExampleAtCaret(example);
        }
    }
}
