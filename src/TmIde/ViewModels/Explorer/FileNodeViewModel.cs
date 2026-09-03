using System;
using System.Collections.ObjectModel;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using TmIde.ViewModels;

namespace TmIde.ViewModels.Explorer;

/// <summary>
/// Nodo del árbol del explorador de archivos. Puede ser carpeta (con
/// <see cref="Children"/>) o archivo.
/// </summary>

public sealed partial class FileNodeViewModel : ViewModelBase
{
    private readonly Action<FileNodeViewModel>? _onOpen;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isVisible = true;

    public FileNodeViewModel(string name, bool isDirectory, Action<FileNodeViewModel>? onOpen, params FileNodeViewModel[] children)
    {
        Name = name;
        IsDirectory = isDirectory;
        _onOpen = onOpen;

        Children = new ObservableCollection<FileNodeViewModel>(children);
    }

    public string Name { get; }

    public bool IsDirectory { get; }

    public ObservableCollection<FileNodeViewModel> Children { get; }

    /// <summary>Clave de geometría en Icons.xaml según tipo / estado / extensión.</summary>
    public string IconKey
    {
        get
        {
            if (IsDirectory)
            {
                return IsExpanded ? "IconFolderOpen" : "IconFolder";
            }

            var ext = System.IO.Path.GetExtension(Name).ToLowerInvariant();
            return ext is ".tm" or ".tmpr" or ".ts" or ".tsx" or ".js" ? "IconFileCode" : "IconFile";
        }
    }

    [RelayCommand]
    private void Open()
    {
        if (IsDirectory)
        {
            IsExpanded = !IsExpanded;
            return;
        }

        _onOpen?.Invoke(this);
    }

    partial void OnIsExpandedChanged(bool value) => OnPropertyChanged(nameof(IconKey));

    /// <summary>
    /// Marca la visibilidad de este nodo y sus descendientes según un filtro de
    /// texto. Un nodo es visible si su nombre contiene el texto o si algún
    /// descendiente es visible (en cuyo caso además se expande).
    /// </summary>
    public bool ApplyFilter(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            IsVisible = true;
            foreach (var child in Children)
            {
                child.ApplyFilter(text);
            }

            return true;
        }

        var selfMatch = Name.Contains(text, StringComparison.OrdinalIgnoreCase);
        var anyChildVisible = false;
        foreach (var child in Children)
        {
            anyChildVisible |= child.ApplyFilter(text);
        }

        IsVisible = selfMatch || anyChildVisible;
        if (anyChildVisible)
        {
            IsExpanded = true;
        }

        return IsVisible;
    }
}
