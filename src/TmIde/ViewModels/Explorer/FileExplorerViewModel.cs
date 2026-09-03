using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using TmIde.ViewModels;

namespace TmIde.ViewModels.Explorer;

/// <summary>
/// Panel EXPLORE: caja de búsqueda + árbol de archivos del proyecto. El árbol
/// arranca vacío.
/// </summary>

public sealed partial class FileExplorerViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _searchText = string.Empty;

    public FileExplorerViewModel()
    {
        Roots = new ObservableCollection<FileNodeViewModel>();
    }

    public ObservableCollection<FileNodeViewModel> Roots { get; }

    public string HeaderTitle => "EXPLORE";

    partial void OnSearchTextChanged(string value)
    {
        foreach (var root in Roots)
        {
            root.ApplyFilter(value);
        }
    }
}
