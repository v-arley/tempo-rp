using Tempo.Language.Diagnostics;
using Tempo.Language.Evaluation;
using Xunit;

namespace Tempo.Language.Tests.Evaluation;

public sealed class RuntimeDiagnosticFormatTests
{
    [Theory]
    [InlineData(RuntimeErrorCodes.DivisionByZero)]
    [InlineData(RuntimeErrorCodes.IndexOutOfBounds)]
    [InlineData(RuntimeErrorCodes.ArithmeticOverflow)]
    [InlineData(RuntimeErrorCodes.InputParseError)]
    [InlineData(RuntimeErrorCodes.UndefinedFunction)]
    [InlineData(RuntimeErrorCodes.InputEndOfFile)]
    [InlineData(RuntimeErrorCodes.InvalidToneInput)]
    public void RuntimeError_ProducesStandardFiveFieldFormat(
        string expectedCode)
    {
        var exception = new TmRuntimeException(
            expectedCode,
            3,
            "Error de ejecucion de prueba.");

        string title = DiagnosticCatalog.TitleOf(
            exception.Code);

        Assert.NotEqual(
            "Diagnostico sin catalogar",
            title);

        var diagnostic = new Diagnostic(
            DiagnosticSeverity.Error,
            exception.Code,
            title,
            "test.tmpr",
            exception.Line,
            exception.Message,
            "Revise los datos de entrada.");

        string[] lines = diagnostic
            .ToStandardFormat()
            .Split(
                Environment.NewLine,
                StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(5, lines.Length);

        Assert.Equal(
            $"[ERROR {expectedCode}] {title}",
            lines[0]);

        Assert.Equal(
            "Archivo: test.tmpr",
            lines[1]);

        Assert.Equal(
            "Línea: 3",
            lines[2]);

        Assert.Equal(
            "Descripción: Error de ejecucion de prueba.",
            lines[3]);

        Assert.Equal(
            "Posible solución: Revise los datos de entrada.",
            lines[4]);
    }
}