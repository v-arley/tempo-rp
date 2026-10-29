
using TmIde.Controls;

using Xunit;

namespace TmIde.Tests.ViewModels;

public sealed class OutputLogColorizerTests
{
    [Theory]
    [InlineData(
        "── IMPORTACIÓN ──",
        "SyntaxNumberBrush")]
    [InlineData(
        "── ANÁLISIS LÉXICO ──",
        "SyntaxLiteralBrush")]
    [InlineData(
        "── ANÁLISIS SINTÁCTICO ──",
        "SyntaxStringBrush")]
    [InlineData(
        "── ANÁLISIS SEMÁNTICO ──",
        "SyntaxTypeBrush")]
    [InlineData(
        "── EJECUCIÓN ──",
        "DiagnosticErrorBrush")]
    [InlineData(
        "── ADVERTENCIAS ──",
        "DiagnosticWarningBrush")]
    [InlineData(
        "── OTROS DIAGNÓSTICOS ──",
        "SecondaryForegroundBrush")]
    public void Category_UsesExpectedBrush(
        string text,
        string expectedBrush)
    {
        var result =
            OutputLogColorizer.GetBrushKeyForLine(text);

        Assert.Equal(expectedBrush, result);
    }

    [Fact]
    public void Error_UsesErrorBrush()
    {
        var result =
            OutputLogColorizer.GetBrushKeyForLine(
                "[ERROR E001] Identificador no declarado");

        Assert.Equal(
            "DiagnosticErrorBrush",
            result);
    }

    [Fact]
    public void Warning_UsesWarningBrush()
    {
        var result =
            OutputLogColorizer.GetBrushKeyForLine(
                "[ADVERTENCIA W001] Variable no utilizada");

        Assert.Equal(
            "DiagnosticWarningBrush",
            result);
    }

    [Fact]
    public void SuccessfulCompilation_UsesSuccessBrush()
    {
        var result =
            OutputLogColorizer.GetBrushKeyForLine(
                "COMPILE → Compilación exitosa.");

        Assert.Equal(
            "SyntaxNumberBrush",
            result);
    }

    [Fact]
    public void FailedCompilation_UsesErrorBrush()
    {
        var result =
            OutputLogColorizer.GetBrushKeyForLine(
                "COMPILE → Compilación con 2 error(es).");

        Assert.Equal(
            "DiagnosticErrorBrush",
            result);
    }

    [Theory]
    [InlineData("COMPILE → Iniciando...")]
    [InlineData("Archivo: principal.tmpr")]
    [InlineData("Línea: 5")]
    [InlineData("Descripción: Error de prueba")]
    [InlineData("Mensaje normal de Output")]
    [InlineData("")]
    public void NormalLines_DoNotUseCategoryColor(
        string text)
    {
        var result =
            OutputLogColorizer.GetBrushKeyForLine(text);

        Assert.Null(result);
    }
}
