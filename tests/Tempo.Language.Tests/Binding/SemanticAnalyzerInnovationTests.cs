using Tempo.Language.Diagnostics;
using Xunit;
using static Tempo.Language.Tests.Binding.SemanticTestHarness;

namespace Tempo.Language.Tests.Binding;

/// <summary>
/// Semántica estática de la innovación: rango de <c>tempo</c>
/// (E040) y tipo de la intensidad de <c>pulse</c> (E042).
/// </summary>

public sealed class SemanticAnalyzerInnovationTests
{
    [Theory]
    [InlineData("64")]
    [InlineData("96")]
    [InlineData("300")]
    public void Tempo_InRange_HasNoErrors(string bpm)
    {
        IReadOnlyList<Diagnostic> diagnostics = Analyze($"tempo {bpm};\nsession > <");

        Assert.Empty(ErrorsOf(diagnostics));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("20")]
    [InlineData("63")]
    [InlineData("301")]
    [InlineData("500")]
    [InlineData("99999999999")]
    public void Tempo_OutOfRange_ReportsE040(string bpm)
    {
        IReadOnlyList<Diagnostic> diagnostics = Analyze($"tempo {bpm};\nsession > <");

        Diagnostic error = Single(diagnostics, DiagnosticCodes.TempoOutOfRange);
        Assert.Equal(1, error.Line);
    }

    [Theory]
    [InlineData("pulse [50];")]
    [InlineData("pulse [12.5];")]
    [InlineData("sample raiz = 40;\n    pulse [raiz + 7];")]
    [InlineData("wave nivel = 0.5;\n    pulse [nivel * 100.0];")]
    public void Pulse_NumericIntensity_HasNoErrors(string body)
    {
        IReadOnlyList<Diagnostic> diagnostics = AnalyzeSession(body);

        Assert.Empty(ErrorsOf(diagnostics));
    }

    [Theory]
    [InlineData("pulse [\"fuerte\"];")]
    [InlineData("pulse [true];")]
    [InlineData("pulse ['a'];")]
    [InlineData("sequence<sample> notas = [1, 2];\n    pulse [notas];")]
    public void Pulse_NonNumericIntensity_ReportsE042(string body)
    {
        IReadOnlyList<Diagnostic> diagnostics = AnalyzeSession(body);

        Assert.Equal(new[] { DiagnosticCodes.NonNumericPulseIntensity }, CodesOf(ErrorsOf(diagnostics)));
    }

    [Fact]
    public void Pulse_UndeclaredIdentifier_ReportsOnlyE001()
    {
        IReadOnlyList<Diagnostic> diagnostics = AnalyzeSession("pulse [falta];");

        Assert.Equal(new[] { DiagnosticCodes.UndeclaredIdentifier }, CodesOf(ErrorsOf(diagnostics)));
    }

    [Fact]
    public void Pulse_UsesVariable_NoUnusedWarning()
    {
        IReadOnlyList<Diagnostic> diagnostics = AnalyzeSession("sample raiz = 40;\n    pulse [raiz];");

        Assert.False(HasCode(diagnostics, DiagnosticCodes.UnusedVariable));
    }

    [Fact]
    public void CanonicalErrorExample_ReportsE040AndE042()
    {
        IReadOnlyList<Diagnostic> diagnostics = Analyze(
            "tempo 500;\n\nsession >\n    pulse [\"fuerte\"];\n<");

        Assert.Equal(1, Single(diagnostics, DiagnosticCodes.TempoOutOfRange).Line);
        Assert.Equal(4, Single(diagnostics, DiagnosticCodes.NonNumericPulseIntensity).Line);
    }
}
