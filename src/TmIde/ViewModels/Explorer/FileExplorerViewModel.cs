using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using TmIde.Services;
using TmIde.ViewModels;
using TmIde.ViewModels.Editor;

namespace TmIde.ViewModels.Explorer;

public sealed partial class FileExplorerViewModel : ViewModelBase
{
    private static readonly HashSet<string> IgnoredDirectories =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "bin",
            "obj"
        };

    private readonly IMessenger _messenger;
    private readonly IProjectFileService _fileService;
    private readonly INamePromptService _namePrompt;
    private readonly IOutputLog _output;
    private EditorHostViewModel? _editorHost;
    private IConfirmDialogService? _confirmDialog;

    private string? _rootDirectory;

    [ObservableProperty]
    private FileNodeViewModel? _selectedNode;

    public FileExplorerViewModel(
        IMessenger messenger,
        IProjectFileService fileService,
        INamePromptService namePrompt,
        IOutputLog output)
    {
        _messenger = messenger;
        _fileService = fileService;
        _namePrompt = namePrompt;
        _output = output;

        Roots = new ObservableCollection<FileNodeViewModel>();
    }

    public void ConfigureFileOperations( EditorHostViewModel editorHost, IConfirmDialogService confirmDialog)
    {
        _editorHost = editorHost;
        _confirmDialog = confirmDialog;
    }

    public ObservableCollection<FileNodeViewModel> Roots { get; }

    public string HeaderTitle => "EXPLORADOR";

    public void Load(string rootDirectory)
    {
        var fullPath = Path.GetFullPath(rootDirectory);

        var root = CreateDirectoryNode(fullPath);
        root.IsExpanded = true;

        _rootDirectory = fullPath;
        SelectedNode = null;

        Roots.Clear();
        Roots.Add(root);
    }

    public void Refresh(string? selectPath = null)
    {
        if (_rootDirectory is null)
        {
            return;
        }

        var expandedPaths = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var root in Roots)
        {
            CollectExpandedPaths(root, expandedPaths);
        }

        var selectedPath =
            selectPath ?? SelectedNode?.FullPath;

        var newRoot = CreateDirectoryNode(_rootDirectory);
        newRoot.IsExpanded = true;

        RestoreExpandedPaths(newRoot, expandedPaths);

        FileNodeViewModel? newSelection = null;

        if (selectedPath is not null)
        {
            newSelection = FindNode(newRoot, selectedPath);

            if (newSelection is not null)
            {
                TryExpandToNode(newRoot, selectedPath);
            }
        }

        Roots.Clear();
        SelectedNode = null;

        Roots.Add(newRoot);

        if (newSelection is not null)
        {
            SelectedNode = newSelection;
            newSelection.IsSelected = true;
        }
    }

    [RelayCommand]
    private void CreateFile()
    {
        CreateItem(isDirectory: false);
    }

    [RelayCommand]
    private void CreateFolder()
    {
        CreateItem(isDirectory: true);
    }

    private void CreateItem(bool isDirectory)
    {
        if (_rootDirectory is null)
        {
            Report("Abre un proyecto primero.");
            return;
        }

        var targetDirectory = GetTargetDirectory();

        var name = _namePrompt.RequestName(
            isDirectory ? "Nueva carpeta" : "Nuevo archivo",
            isDirectory
                ? "Nombre de la nueva carpeta:"
                : "Nombre del nuevo archivo (.tm o .tmpr):",
            isDirectory ? "nueva_carpeta" : "nuevo.tm");

        if (name is null)
        {
            return;
        }

        try
        {
            var createdPath = isDirectory
                ? _fileService.CreateFolder(
                    _rootDirectory,
                    targetDirectory,
                    name)
                : _fileService.CreateFile(
                    _rootDirectory,
                    targetDirectory,
                    name);

            Refresh(createdPath);

            Report(isDirectory
                ? $"Carpeta creada: {createdPath}"
                : $"Archivo creado: {createdPath}");

            if (!isDirectory)
            {
                _messenger.Send(
                    new OpenFileRequestedMessage(createdPath));
            }
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or InvalidOperationException
            or IOException
            or UnauthorizedAccessException)
        {
            Report($"No se pudo crear: {exception.Message}");
        }
    }

    private string GetTargetDirectory()
    {
        if (_rootDirectory is null)
        {
            throw new InvalidOperationException(
                "No hay un proyecto abierto.");
        }

        if (SelectedNode is null)
        {
            return _rootDirectory;
        }

        if (SelectedNode.IsDirectory)
        {
            return SelectedNode.FullPath;
        }

        return Path.GetDirectoryName(SelectedNode.FullPath)
            ?? _rootDirectory;
    }

    private FileNodeViewModel CreateDirectoryNode(string directory)
    {
        var subdirectories = Directory
            .EnumerateDirectories(directory)
            .Where(path => !IsIgnored(path))
            .OrderBy(
                path => path,
                StringComparer.OrdinalIgnoreCase)
            .Select(CreateDirectoryNode);

        var files = Directory
            .EnumerateFiles(directory)
            .Where(path => !IsIgnored(path))
            .OrderBy(
                path => path,
                StringComparer.OrdinalIgnoreCase)
            .Select(path => new FileNodeViewModel(
                path,
                isDirectory: false,
                OpenFile,
                []));

        return new FileNodeViewModel(
            directory,
            isDirectory: true,
            onOpen: null,
            subdirectories.Concat(files).ToList());
    }

    private static bool IsIgnored(string path)
    {
        var name = Path.GetFileName(path);

        return name.StartsWith('.') ||
               IgnoredDirectories.Contains(name);
    }

    private void OpenFile(FileNodeViewModel node)
    {
        _messenger.Send(
            new OpenFileRequestedMessage(node.FullPath));
    }

    private static void CollectExpandedPaths(
        FileNodeViewModel node,
        HashSet<string> paths)
    {
        if (node.IsExpanded)
        {
            paths.Add(node.FullPath);
        }

        foreach (var child in node.Children)
        {
            CollectExpandedPaths(child, paths);
        }
    }

    private static void RestoreExpandedPaths(
        FileNodeViewModel node,
        HashSet<string> paths)
    {
        node.IsExpanded = paths.Contains(node.FullPath);

        foreach (var child in node.Children)
        {
            RestoreExpandedPaths(child, paths);
        }
    }

    private static FileNodeViewModel? FindNode(
        FileNodeViewModel node,
        string path)
    {
        if (string.Equals(
            node.FullPath,
            path,
            StringComparison.OrdinalIgnoreCase))
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var found = FindNode(child, path);

            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static bool TryExpandToNode(
        FileNodeViewModel node,
        string path)
    {
        if (string.Equals(
            node.FullPath,
            path,
            StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var child in node.Children)
        {
            if (TryExpandToNode(child, path))
            {
                node.IsExpanded = true;
                return true;
            }
        }

        return false;
    }

    [RelayCommand]
    private void RenameSelected()
    {
        if (_rootDirectory is null ||
            SelectedNode is null ||
            _editorHost is null ||
            _confirmDialog is null)
        {
            return;
        }

        var node = SelectedNode;

        if (string.Equals(
            node.FullPath,
            _rootDirectory,
            StringComparison.OrdinalIgnoreCase))
        {
            Report("No se puede renombrar la raíz del proyecto.");
            return;
        }

        var newName = _namePrompt.RequestName(
            "Renombrar",
            "Escribe el nuevo nombre:",
            node.Name);

        if (newName is null ||
            string.Equals(newName, node.Name, StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            var affectedDocuments =
                _editorHost.DocumentsAtOrUnder(node.FullPath);

            var modifiedDocuments = affectedDocuments
                .Where(document => document.IsModified)
                .ToList();

            if (modifiedDocuments.Count > 0)
            {
                var confirmed = _confirmDialog.Confirm(
                    "Hay documentos abiertos con cambios sin guardar.\n\n" +
                    "¿Deseas guardar esos cambios antes de renombrar?");

                if (!confirmed)
                {
                    return;
                }

                foreach (var document in modifiedDocuments)
                {
                    document.Save();
                }
            }

            var oldPath = node.FullPath;

            var newPath = _fileService.Rename(
                _rootDirectory,
                oldPath,
                newName);

            _editorHost.ApplyPathRename(oldPath, newPath);

            Refresh(newPath);

            Report($"Renombrado correctamente: {newName}");
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or InvalidOperationException
            or IOException
            or UnauthorizedAccessException)
        {
            Report($"No se pudo renombrar: {exception.Message}");
        }
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        if (_rootDirectory is null ||
            SelectedNode is null ||
            _editorHost is null ||
            _confirmDialog is null)
        {
            return;
        }

        var node = SelectedNode;

        if (string.Equals(
            node.FullPath,
            _rootDirectory,
            StringComparison.OrdinalIgnoreCase))
        {
            Report("No se puede eliminar la raíz del proyecto.");
            return;
        }

        var affectedDocuments =
            _editorHost.DocumentsAtOrUnder(node.FullPath);

        var hasUnsavedChanges =
            affectedDocuments.Any(document => document.IsModified);

        var message = node.IsDirectory
            ? $"¿Deseas eliminar la carpeta '{node.Name}' y todo su contenido?"
            : $"¿Deseas eliminar el archivo '{node.Name}'?";

        if (hasUnsavedChanges)
        {
            message +=
                "\n\nADVERTENCIA: Hay cambios sin guardar " +
                "que se perderán si continúas.";
        }

        message += "\n\nEsta operación no se puede deshacer desde Tempo.";

        if (!_confirmDialog.Confirm(message))
        {
            return;
        }

        try
        {
            var path = node.FullPath;

            var parentDirectory =
                Path.GetDirectoryName(path) ?? _rootDirectory;

            _fileService.Delete(_rootDirectory, path);

            _editorHost.CloseDocumentsAtOrUnder(path);

            Refresh(parentDirectory);

            Report($"Eliminado correctamente: {node.Name}");
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or InvalidOperationException
            or IOException
            or UnauthorizedAccessException)
        {
            Report($"No se pudo eliminar: {exception.Message}");
        }
    }

    /// <summary>Escribe en el panel de salida un mensaje del explorador, precedido de su título.</summary>
    private void Report(string message) => _output.WriteLine($"{HeaderTitle} → {message}");
}