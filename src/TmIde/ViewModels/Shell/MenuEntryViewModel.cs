using System;
using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.Input;

using TmIde.ViewModels;

namespace TmIde.ViewModels.Shell;

/// <summary>
/// Entrada de menú: puede ser un contenedor (con <see cref="Items"/>) o una
/// hoja con acción (<see cref="Command"/>). Usada por la barra de menú del
/// título (MENU / TOOLS / VER / OTROS) y sus submenús.
/// </summary>

public sealed partial class MenuEntryViewModel : ViewModelBase
{
    public MenuEntryViewModel(string header, IRelayCommand? command = null, string shortcut = "")
    {
        Header = header;
        Command = command;
        Shortcut = shortcut;
    }

    public MenuEntryViewModel(string header, Action action, string shortcut = "")
        : this(header, new RelayCommand(action), shortcut)
    {
    }

    public string Header { get; }

    /// <summary>Atajo de teclado que se muestra alineado a la derecha ("Ctrl+S"); vacío si no tiene.</summary>
    public string Shortcut { get; }

    public bool HasShortcut => Shortcut.Length > 0;

    public IRelayCommand? Command { get; }

    public ObservableCollection<MenuEntryViewModel> Items { get; } = new();

    public bool HasItems => Items.Count > 0;

    public MenuEntryViewModel Add(MenuEntryViewModel child)
    {
        Items.Add(child);
        return this;
    }
}
