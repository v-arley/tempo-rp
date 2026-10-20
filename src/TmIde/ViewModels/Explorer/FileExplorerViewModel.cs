using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using TmIde.ViewModels;

namespace TmIde.ViewModels.Explorer;

/// <summary>
/// Panel EXPLORE: caja de búsqueda + árbol de archivos del proyecto. El árbol
/// arranca vacío y se llena con <see cref="Load"/> al abrir un proyecto.
/// </summary>

public sealed partial class FileExplorerViewModel : ViewModelBase
{
    // Carpetas de compilación: no forman parte del proyecto.
    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase) { "bin", "obj" };

    [ObservableProperty]
    private string _searchText = string.Empty;

    private Action<string>? _openFile;

    public FileExplorerViewModel()
    {
        Roots = new ObservableCollection<FileNodeViewModel>();
    }

    public ObservableCollection<FileNodeViewModel> Roots { get; }

    public string HeaderTitle => "EXPLORE";

    /// <summary>
    /// Sustituye el árbol por el de <paramref name="rootDirectory"/>, con la
    /// raíz expandida. Abrir un archivo llama a <paramref name="openFile"/> con su
    /// ruta completa.
    /// </summary>
    public void Load(string rootDirectory, Action<string> openFile)
    {
        _openFile = openFile;

        Roots.Clear();
        var root = CreateDirectoryNode(rootDirectory);
        root.IsExpanded = true;
        Roots.Add(root);

        root.ApplyFilter(SearchText);
    }

    private FileNodeViewModel CreateDirectoryNode(string directory)
    {
        var subdirectories = Directory.EnumerateDirectories(directory)
            .Where(path => !IsIgnored(path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(CreateDirectoryNode);

        var files = Directory.EnumerateFiles(directory)
            .Where(path => !IsIgnored(path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => new FileNodeViewModel(path, isDirectory: false, OpenFile, []));

        return new FileNodeViewModel(directory, isDirectory: true, onOpen: null, subdirectories.Concat(files).ToList());
    }

    // Ocultos (.git, .vs) y carpetas de compilación.
    private static bool IsIgnored(string path)
    {
        var name = Path.GetFileName(path);
        return name.StartsWith('.') || IgnoredDirectories.Contains(name);
    }

    private void OpenFile(FileNodeViewModel node) => _openFile?.Invoke(node.FullPath);

    partial void OnSearchTextChanged(string value)
    {
        foreach (var root in Roots)
        {
            root.ApplyFilter(value);
        }
    }
}
