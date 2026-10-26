using System;
using System.IO;

using TmIde.Services;

using Xunit;

namespace TmIde.Tests.Services;

public sealed class ProjectFileServiceTests : IDisposable
{
    private readonly TempDirectory _project = TempDirectory.Create();
    private readonly IProjectFileService _files = new ProjectFileService();

    public void Dispose()
    {
        _project.Dispose();
    }

    [Fact]
    public void CreateFile_Module_CreatesEmptyFile()
    {
        var path = _files.CreateFile(
            _project.Path,
            _project.Path,
            "operaciones.tm");

        Assert.True(File.Exists(path));
        Assert.Equal(string.Empty, File.ReadAllText(path));
    }

    [Fact]
    public void CreateFile_Main_CreatesSession()
    {
        var path = _files.CreateFile(
            _project.Path,
            _project.Path,
            "principal.tmpr");

        Assert.Contains("session >", File.ReadAllText(path));
    }

    [Fact]
    public void CreateFile_SecondMain_ReportsE017()
    {
        _project.WriteFile("principal.tmpr", "session > <");

        var error = Assert.Throws<InvalidOperationException>(() =>
            _files.CreateFile(
                _project.Path,
                _project.Path,
                "otro.tmpr"));

        Assert.Contains("E017", error.Message);
        Assert.False(File.Exists(
            Path.Combine(_project.Path, "otro.tmpr")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("archivo?.tm")]
    [InlineData("../salir.tm")]
    [InlineData("CON.tm")]
    public void CreateFile_InvalidName_IsRejected(string name)
    {
        Assert.Throws<ArgumentException>(() =>
            _files.CreateFile(
                _project.Path,
                _project.Path,
                name));
    }

    [Theory]
    [InlineData("archivo.js")]
    [InlineData("archivo.txt")]
    [InlineData("sin-extension")]
    public void CreateFile_InvalidExtension_IsRejected(string name)
    {
        Assert.Throws<ArgumentException>(() =>
            _files.CreateFile(
                _project.Path,
                _project.Path,
                name));
    }

    [Fact]
    public void CreateFile_ExistingFile_DoesNotOverwrite()
    {
        var existing = _project.WriteFile(
            "operaciones.tm",
            "contenido original");

        Assert.Throws<IOException>(() =>
            _files.CreateFile(
                _project.Path,
                _project.Path,
                "operaciones.tm"));

        Assert.Equal(
            "contenido original",
            File.ReadAllText(existing));
    }

    [Fact]
    public void CreateFolder_CreatesDirectory()
    {
        var folder = _files.CreateFolder(
            _project.Path,
            _project.Path,
            "modulos");

        Assert.True(Directory.Exists(folder));
    }

    [Fact]
    public void CreateFile_NestedModule_Works()
    {
        var folder = _files.CreateFolder(
            _project.Path,
            _project.Path,
            "modulos");

        var file = _files.CreateFile(
            _project.Path,
            folder,
            "utilidades.tm");

        Assert.True(File.Exists(file));
    }

    [Fact]
    public void Rename_Module_PreservesContent()
    {
        var original = _project.WriteFile(
            "operaciones.tm",
            "contenido");

        var renamed = _files.Rename(
            _project.Path,
            original,
            "utilidades.tm");

        Assert.False(File.Exists(original));
        Assert.True(File.Exists(renamed));
        Assert.Equal("contenido", File.ReadAllText(renamed));
    }

    [Fact]
    public void Rename_ModuleToSecondMain_ReportsE017()
    {
        _project.WriteFile("principal.tmpr", "session > <");
        var module = _project.WriteFile("utilidades.tm", "");

        var error = Assert.Throws<InvalidOperationException>(() =>
            _files.Rename(
                _project.Path,
                module,
                "segundo.tmpr"));

        Assert.Contains("E017", error.Message);
        Assert.True(File.Exists(module));
    }

    [Fact]
    public void Delete_File_RemovesIt()
    {
        var file = _project.WriteFile("archivo.tm", "");

        _files.Delete(_project.Path, file);

        Assert.False(File.Exists(file));
    }

    [Fact]
    public void Delete_Folder_RemovesItsContent()
    {
        var folder = _project.CreateDirectory("temporal");
        _project.WriteFile("temporal/archivo.tm", "");

        _files.Delete(_project.Path, folder);

        Assert.False(Directory.Exists(folder));
    }

    [Fact]
    public void Delete_ProjectRoot_IsForbidden()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _files.Delete(_project.Path, _project.Path));

        Assert.True(Directory.Exists(_project.Path));
    }

    [Fact]
    public void CreateFile_OutsideProject_IsForbidden()
    {
        using var other = TempDirectory.Create();

        Assert.Throws<InvalidOperationException>(() =>
            _files.CreateFile(
                _project.Path,
                other.Path,
                "archivo.tm"));
    }

    [Fact]
    public void CreateFile_NestedMain_IsForbidden()
    {
        var folder = _files.CreateFolder(
            _project.Path,
            _project.Path,
            "carpeta");

        Assert.Throws<InvalidOperationException>(() =>
            _files.CreateFile(
                _project.Path,
                folder,
                "principal.tmpr"));
    }
}