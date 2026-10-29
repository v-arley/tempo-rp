
using System;
using System.IO;
using System.Linq;

using Tempo.Language.Diagnostics;

using TmIde.ViewModels.Panel;

using Xunit;

namespace TmIde.Tests.ViewModels;

public sealed class DiagnosticCategoryClassifierTests
{
    [Theory]
    [InlineData("E001", DiagnosticCategory.Semantica)]
    [InlineData("E009", DiagnosticCategory.Semantica)]
    [InlineData("E020", DiagnosticCategory.Semantica)]
    [InlineData("E012", DiagnosticCategory.Importacion)]
    [InlineData("E014", DiagnosticCategory.Importacion)]
    [InlineData("E021", DiagnosticCategory.Lexico)]
    [InlineData("E026", DiagnosticCategory.Lexico)]
    [InlineData("E027", DiagnosticCategory.Sintaxis)]
    [InlineData("E039", DiagnosticCategory.Sintaxis)]
    [InlineData("R001", DiagnosticCategory.Ejecucion)]
    [InlineData("R007", DiagnosticCategory.Ejecucion)]
    [InlineData("W001", DiagnosticCategory.Advertencia)]
    [InlineData("W004", DiagnosticCategory.Advertencia)]
    [InlineData("E999", DiagnosticCategory.Otro)]
    public void Classify_UsesDiagnosticCode(
        string code,
        DiagnosticCategory expected)
    {
        var diagnostic = CreateDiagnostic(code);

        var actual =
            DiagnosticCategoryClassifier.Classify(diagnostic);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WarningSeverity_HasPriority()
    {
        var diagnostic = new Diagnostic(
            DiagnosticSeverity.Warning,
            "E001",
            "Advertencia de prueba",
            "principal.tmpr",
            2,
            "Descripcion",
            "Sugerencia");

        Assert.Equal(
            DiagnosticCategory.Advertencia,
            DiagnosticCategoryClassifier.Classify(diagnostic));
    }

    [Fact]
    public void CategorizedDiagnostic_AddsCorrectHeading()
    {
        var output = new OutputPaneViewModel("Output");

        output.BeginDiagnosticBatch();

        output.AppendCategorizedDiagnostic(
            CreateDiagnostic("E032"),
            Path.GetFullPath("principal.tmpr"));

        Assert.Contains(
            "── ANÁLISIS SINTÁCTICO ──",
            output.Lines);
    }

    [Fact]
    public void SameCategory_DoesNotRepeatHeading()
    {
        var output = new OutputPaneViewModel("Output");

        output.BeginDiagnosticBatch();

        output.AppendCategorizedDiagnostic(
            CreateDiagnostic("E027"),
            null);

        output.AppendCategorizedDiagnostic(
            CreateDiagnostic("E032"),
            null);

        var count = output.Lines.Count(line =>
            line == "── ANÁLISIS SINTÁCTICO ──");

        Assert.Equal(1, count);
    }

    [Fact]
    public void DifferentCategories_AddDifferentHeadings()
    {
        var output = new OutputPaneViewModel("Output");

        output.BeginDiagnosticBatch();

        output.AppendCategorizedDiagnostic(
            CreateDiagnostic("E012"),
            null);

        output.AppendCategorizedDiagnostic(
            CreateDiagnostic("E001"),
            null);

        output.AppendCategorizedDiagnostic(
            CreateDiagnostic("W001"),
            null);

        Assert.Contains(
            "── IMPORTACIÓN ──",
            output.Lines);

        Assert.Contains(
            "── ANÁLISIS SEMÁNTICO ──",
            output.Lines);

        Assert.Contains(
            "── ADVERTENCIAS ──",
            output.Lines);
    }

    [Fact]
    public void RuntimeError_UsesExecutionHeading()
    {
        var output = new OutputPaneViewModel("Output");

        output.AppendCategorizedDiagnostic(
            CreateDiagnostic("R001"),
            Path.GetFullPath("principal.tmpr"));

        Assert.Contains(
            "── EJECUCIÓN ──",
            output.Lines);
    }

    [Fact]
    public void Header_IsNotNavigable_ButDiagnosticIs()
    {
        var output = new OutputPaneViewModel("Output");

        var path = Path.GetFullPath("principal.tmpr");

        output.BeginDiagnosticBatch();
        output.AppendCategorizedDiagnostic(
            CreateDiagnostic("E032"),
            path);

        var lines = output.Lines.ToList();

        var headerLine = lines.FindIndex(line =>
            line == "── ANÁLISIS SINTÁCTICO ──") + 1;

        var diagnosticLine = lines.FindIndex(line =>
            line.StartsWith(
                "[ERROR E032]",
                StringComparison.Ordinal)) + 1;

        Assert.True(headerLine > 0);
        Assert.True(diagnosticLine > 0);

        // El encabezado no debe abrir el editor.
        Assert.False(output.ActivateLine(headerLine));

        DiagnosticNavigationTarget? activated = null;

        output.DiagnosticActivated += target =>
            activated = target;

        // Las cinco lineas originales siguen siendo navegables.
        for (var line = diagnosticLine;
             line < diagnosticLine + 5;
             line++)
        {
            activated = null;

            Assert.True(output.ActivateLine(line));
            Assert.NotNull(activated);
            Assert.Equal(path, activated.FilePath);
            Assert.Equal(4, activated.Line);
            Assert.Equal(7, activated.Column);
        }
    }

    [Fact]
    public void NewBatch_RepeatsHeading()
    {
        var output = new OutputPaneViewModel("Output");

        output.BeginDiagnosticBatch();

        output.AppendCategorizedDiagnostic(
            CreateDiagnostic("E001"),
            null);

        output.BeginDiagnosticBatch();

        output.AppendCategorizedDiagnostic(
            CreateDiagnostic("E002"),
            null);

        var count = output.Lines.Count(line =>
            line == "── ANÁLISIS SEMÁNTICO ──");

        Assert.Equal(2, count);
    }

    private static Diagnostic CreateDiagnostic(string code)
    {
        var severity = code.StartsWith(
            'W')
            ? DiagnosticSeverity.Warning
            : DiagnosticSeverity.Error;

        return new Diagnostic(
            severity,
            code,
            "Diagnostico de prueba",
            "principal.tmpr",
            4,
            "Descripcion de prueba",
            "Sugerencia de prueba")
        {
            Column = 7
        };
    }
}
