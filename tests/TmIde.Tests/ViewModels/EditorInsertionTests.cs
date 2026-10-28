
using System;

using CommunityToolkit.Mvvm.Messaging;

using NSubstitute;

using TmIde.Services.Analysis;
using TmIde.ViewModels.Editor;

using Xunit;

namespace TmIde.Tests.ViewModels;

public sealed class EditorInsertionTests
{
    private static EditorHostViewModel CreateEditor()
    {
        var analysis = Substitute.For<ICodeAnalysisService>();

        analysis
            .Analyze(default!, default!)
            .ReturnsForAnyArgs([]);

        return new EditorHostViewModel(
            analysis,
            new WeakReferenceMessenger());
    }

    [Fact]
    public void Insert_WithoutActiveDocument_ReturnsFalse()
    {
        var editor = CreateEditor();

        var result = editor.InsertExampleAtCaret(
            "sample numero = 10;");

        Assert.False(result);
    }

    [Fact]
    public void Insert_AtCaret_PreservesExistingContent()
    {
        using var project = TempDirectory.Create();

        var editor = CreateEditor();
        editor.SwitchToProject(project.Path);

        var path = project.WriteFile(
            "principal.tmpr",
            "abc\nxyz");

        editor.OpenDocument(path);

        var document = editor.SelectedDocument!;

        document.CaretLine = 1;
        document.CaretColumn = 2;

        var result = editor.InsertExampleAtCaret("123");

        Assert.True(result);
        Assert.Equal(
            "a123bc\nxyz",
            document.Content);
    }

    [Fact]
    public void Insert_UpdatesNavigationRequest()
    {
        using var project = TempDirectory.Create();

        var editor = CreateEditor();
        editor.SwitchToProject(project.Path);

        var path = project.WriteFile(
            "principal.tmpr",
            "abc");

        editor.OpenDocument(path);

        var document = editor.SelectedDocument!;

        document.CaretLine = 1;
        document.CaretColumn = 2;

        editor.InsertExampleAtCaret("XYZ");

        Assert.NotNull(editor.NavigationRequest);

        Assert.Same(
            document.Document,
            editor.NavigationRequest.Document);

        Assert.Equal(
            4,
            editor.NavigationRequest.Offset);
    }

    [Fact]
    public void Insert_DoesNotSaveFileAutomatically()
    {
        using var project = TempDirectory.Create();

        var editor = CreateEditor();
        editor.SwitchToProject(project.Path);

        var path = project.WriteFile(
            "principal.tmpr",
            "original");

        editor.OpenDocument(path);

        editor.InsertExampleAtCaret("nuevo");

        Assert.Equal(
            "original",
            System.IO.File.ReadAllText(path));

        Assert.True(
            editor.SelectedDocument!.IsModified);
    }

    [Fact]
    public void Insert_EmptyText_DoesNotModifyDocument()
    {
        using var project = TempDirectory.Create();

        var editor = CreateEditor();
        editor.SwitchToProject(project.Path);

        var path = project.WriteFile(
            "principal.tmpr",
            "contenido");

        editor.OpenDocument(path);

        var result = editor.InsertExampleAtCaret("");

        Assert.False(result);
        Assert.Equal(
            "contenido",
            editor.SelectedDocument!.Content);
    }
}
