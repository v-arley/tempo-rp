using TmCompiler.Domain.Diagnostics;
using Xunit;

namespace TmCompiler.Tests.Semantic;

/// <summary>
/// El bloque de cinco campos de la §7.1 debe ser idéntico al que producen
/// <c>LexerException</c> y <c>ParserException</c>: un único formato de error
/// en todo el compilador.
/// </summary>

public sealed class DiagnosticTests
{
    [Fact]
    public void ToStandardFormat_ForError_ProducesTheFiveFieldBlock()
    {
        var diagnostic = new Diagnostic(
            DiagnosticSeverity.Error,
            DiagnosticCodes.IncompatibleAssignment,
            DiagnosticCatalog.TitleOf(DiagnosticCodes.IncompatibleAssignment),
            "operaciones.tm",
            15,
            "No se puede asignar un valor de tipo 'wave' a una variable de tipo 'sample'.",
            "Use una funcion de conversion o cambie el tipo de la variable.");

        string[] lines = diagnostic.ToStandardFormat()
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(5, lines.Length);
        Assert.Equal("[ERROR E003] Tipo incompatible en asignacion", lines[0]);
        Assert.Equal("Archivo: operaciones.tm", lines[1]);
        Assert.Equal("Línea: 15", lines[2]);
        Assert.Equal(
            "Descripción: No se puede asignar un valor de tipo 'wave' a una variable de tipo 'sample'.",
            lines[3]);
        Assert.Equal(
            "Posible solución: Use una funcion de conversion o cambie el tipo de la variable.",
            lines[4]);
    }

    [Fact]
    public void ToStandardFormat_ForWarning_UsesTheWarningLabel()
    {
        var diagnostic = new Diagnostic(
            DiagnosticSeverity.Warning,
            DiagnosticCodes.ShadowedIdentifier,
            DiagnosticCatalog.TitleOf(DiagnosticCodes.ShadowedIdentifier),
            "modulo.tm",
            8,
            "Descripcion.",
            "Sugerencia.");

        string first = diagnostic.ToStandardFormat().Split(Environment.NewLine)[0];

        Assert.Equal("[ADVERTENCIA W002] Sombreado de identificador (shadowing)", first);
    }

    [Theory]
    [InlineData("E001", "Identificador no declarado")]
    [InlineData("E009", "Acceso a variable const en escritura")]
    [InlineData("E019", "Matriz con filas de distinto tamano")]
    [InlineData("W004", "Conversion implicita en concatenacion phrase")]
    public void TitleOf_KnownCode_ReturnsSpecificationTitle(string code, string expected)
    {
        Assert.Equal(expected, DiagnosticCatalog.TitleOf(code));
    }

    [Fact]
    public void ReportError_SameCodeLineAndDescriptionTwice_IsRecordedOnce()
    {
        var bag = new DiagnosticBag("test.tmpr");

        bag.ReportError(DiagnosticCodes.UndeclaredIdentifier, 4, "Repetido.", "Sugerencia.");
        bag.ReportError(DiagnosticCodes.UndeclaredIdentifier, 4, "Repetido.", "Sugerencia.");

        Assert.Single(bag.Diagnostics);
    }

    [Fact]
    public void ReportWarning_WithoutErrors_LeavesHasErrorsFalse()
    {
        var bag = new DiagnosticBag("test.tmpr");

        bag.ReportWarning(DiagnosticCodes.UnusedVariable, 2, "Sin usar.", "Sugerencia.");

        Assert.False(bag.HasErrors);
        Assert.Equal(1, bag.WarningCount);
        Assert.Equal(0, bag.ErrorCount);
    }

    [Fact]
    public void ReportError_UsesTheFileNameGivenToTheBag()
    {
        var bag = new DiagnosticBag("principal.tmpr");

        bag.ReportError(DiagnosticCodes.UndeclaredIdentifier, 1, "Descripcion.", "Sugerencia.");

        Assert.Equal("principal.tmpr", bag.Diagnostics[0].FileName);
        Assert.True(bag.HasErrors);
    }
}
