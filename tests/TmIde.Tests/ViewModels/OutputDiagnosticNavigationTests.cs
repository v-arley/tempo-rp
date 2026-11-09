using System;
using System.IO;
using CommunityToolkit.Mvvm.Messaging;
using NSubstitute;
using Tempo.Language.Diagnostics;
using TmIde.Services.Analysis;
using TmIde.ViewModels.Editor;
using TmIde.ViewModels.Panel;
using Xunit;

namespace TmIde.Tests.ViewModels;

public sealed class OutputDiagnosticNavigationTests
{
    private static Diagnostic CreateDiagnostic(
        int line = 4,
        int? column = 7)
    {
        return new Diagnostic(
            DiagnosticSeverity.Error,
            "E025",
            "Error de sintaxis",
            "principal.tmpr",
            line,
            "Expresion invalida.",
            "Corrige la expresion.")
        {
            Column = column
        };
    }

    [Fact]
    public void Diagnostic_RegistersAllItsLines()
    {
        var output = new OutputPaneViewModel("Output");

        output.AppendLine("COMPILE → Iniciando");

        var path = Path.GetFullPath("principal.tmpr");

        output.AppendDiagnostic(
            CreateDiagnostic(),
            path);

        DiagnosticNavigationTarget? activated = null;

        output.DiagnosticActivated += target =>
            activated = target;

        // La primera línea contiene el mensaje COMPILE.
        Assert.False(output.TryActivateLine(1));

        // El diagnóstico estándar ocupa cinco líneas.
        for (var line = 2; line <= 6; line++)
        {
            activated = null;

            Assert.True(output.TryActivateLine(line));
            Assert.NotNull(activated);
            Assert.Equal(path, activated.FilePath);
            Assert.Equal(4, activated.Line);
            Assert.Equal(7, activated.Column);
        }
    }

    [Fact]
    public void Diagnostic_WithoutFile_IsNotNavigable()
    {
        var output = new OutputPaneViewModel("Output");

        output.AppendDiagnostic(
            CreateDiagnostic(),
            null);

        Assert.False(output.TryActivateLine(1));
        Assert.False(output.TryActivateLine(2));
    }

    [Fact]
    public void NormalOutputMessage_IsNotNavigable()
    {
        var output = new OutputPaneViewModel("Output");

        output.AppendLine("Compilación exitosa");

        Assert.False(output.TryActivateLine(1));
    }

    [Fact]
    public void ClearDiagnosticLinks_RemovesOldLocations()
    {
        var output = new OutputPaneViewModel("Output");

        output.AppendDiagnostic(
            CreateDiagnostic(),
            Path.GetFullPath("principal.tmpr"));

        Assert.True(output.TryActivateLine(1));

        var originalCount = output.Lines.Count;

        output.ClearDiagnosticLinks();

        Assert.False(output.TryActivateLine(1));

        // El historial visible debe conservarse.
        Assert.Equal(originalCount, output.Lines.Count);
    }

    [Fact]
    public void NavigateToDiagnostic_OpensCorrectFile()
    {
        using var project = TempDirectory.Create();

        var path = project.WriteFile(
            "principal.tmpr",
            "primera\nsegunda\ntercera");

        var editor = CreateEditor(project.Path);

        editor.NavigateToDiagnostic(
            path,
            line: 2,
            column: 4);

        Assert.NotNull(editor.SelectedDocument);
        Assert.Equal(path, editor.SelectedDocument.FilePath);

        var request = Assert.IsType<EditorNavigationRequest>(
            editor.NavigationRequest);

        var location = request.Document.GetLocation(
            request.Offset);

        Assert.Equal(2, location.Line);
        Assert.Equal(4, location.Column);
    }

    [Fact]
    public void NavigateToDiagnostic_UnknownColumn_UsesColumnOne()
    {
        using var project = TempDirectory.Create();

        var path = project.WriteFile(
            "principal.tmpr",
            "primera\nsegunda\ntercera");

        var editor = CreateEditor(project.Path);

        editor.NavigateToDiagnostic(
            path,
            line: 2,
            column: null);

        var request = Assert.IsType<EditorNavigationRequest>(
            editor.NavigationRequest);

        var location = request.Document.GetLocation(
            request.Offset);

        Assert.Equal(2, location.Line);
        Assert.Equal(1, location.Column);
    }

    [Fact]
    public void NavigateToDiagnostic_InvalidPosition_IsClamped()
    {
        using var project = TempDirectory.Create();

        var path = project.WriteFile(
            "principal.tmpr",
            "primera\nsegunda");

        var editor = CreateEditor(project.Path);

        editor.NavigateToDiagnostic(
            path,
            line: 999,
            column: 999);

        var request = Assert.IsType<EditorNavigationRequest>(
            editor.NavigationRequest);

        var location = request.Document.GetLocation(
            request.Offset);

        Assert.Equal(2, location.Line);
        Assert.Equal(8, location.Column);
    }

    [Fact]
    public void NavigateToDiagnostic_ReusesOpenDocument()
    {
        using var project = TempDirectory.Create();

        var path = project.WriteFile(
            "principal.tmpr",
            "primera\nsegunda");

        var editor = CreateEditor(project.Path);

        editor.OpenDocument(path);

        var originalDocument = editor.SelectedDocument;

        editor.NavigateToDiagnostic(path, 2, 1);

        Assert.Single(editor.Documents);
        Assert.Same(originalDocument, editor.SelectedDocument);
    }

    private static EditorHostViewModel CreateEditor(
        string projectDirectory)
    {
        var analysis = Substitute.For<ICodeAnalysisService>();

        analysis
            .Analyze(default!, default!)
            .ReturnsForAnyArgs(DocumentAnalysis.Empty);

        var editor = new EditorHostViewModel(
            analysis,
            new WeakReferenceMessenger());

        editor.SwitchToProject(projectDirectory);

        return editor;
    }
}
