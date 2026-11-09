using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
    private bool _isSelected;

    public FileNodeViewModel(string fullPath, bool isDirectory, Action<FileNodeViewModel>? onOpen, IEnumerable<FileNodeViewModel> children)
    {
        FullPath = fullPath;
        Name = System.IO.Path.GetFileName(fullPath);
        IsDirectory = isDirectory;
        _onOpen = onOpen;

        Children = new ObservableCollection<FileNodeViewModel>(children);
    }

    /// <summary>Ruta completa en disco.</summary>
    public string FullPath { get; }

    public string Name { get; }

    public bool IsDirectory { get; }

    /// <summary>
    /// Identifica los archivos principales de Tempo (.tmpr).
    /// </summary>
    public bool IsMainProjectFile => !IsDirectory && string.Equals(System.IO.Path.GetExtension(Name), ".tmpr", StringComparison.OrdinalIgnoreCase);

    public ObservableCollection<FileNodeViewModel> Children { get; }

    /// <summary>
    /// Devuelve el icono correspondiente al tipo de archivo.
    /// </summary>
    public string IconKey
    {
        get
        {
            if (IsDirectory)
            {
                return IsExpanded
                    ? "IconFolderOpen"
                    : "IconFolder";
            }

            var extension = System.IO.Path
                .GetExtension(Name)
                .ToLowerInvariant();

            return extension switch
            {
                ".tmpr" => "IconMainFile",
                ".tm" => "IconFileCode",
                _ => "IconFile"
            };
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
}
