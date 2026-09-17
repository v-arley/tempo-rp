using TmCompiler.Domain.Diagnostics;
using Xunit;

namespace TmCompiler.Tests.Semantic;

/// <summary>
/// Sentencias de control: condiciones booleanas (E005, §5.6) y reglas de
/// <c>mix</c> (§5.5).
/// </summary>

public sealed class SemanticAnalyzerControlFlowTests
{
    [Fact]
    public void Analyze_IfWithSampleCondition_ReportsNonBooleanCondition()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sample x = 5;
                if [x] >
                    output >> x;
                <
            """);

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.NonBooleanCondition));
    }

    [Fact]
    public void Analyze_ElseIfWithNonBooleanCondition_ReportsNonBooleanCondition()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sample x = 5;
                if [x > 1] >
                    output >> x;
                <
                else if [x + 1] >
                    output >> x;
                <
            """);

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.NonBooleanCondition));
    }

    [Fact]
    public void Analyze_RepeatWithNonBooleanCondition_ReportsNonBooleanCondition()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sample x = 5;
                repeat [x] >
                    x++;
                <
            """);

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.NonBooleanCondition));
    }

    [Fact]
    public void Analyze_LoopWithNonBooleanCondition_ReportsNonBooleanCondition()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                loop [sample i = 0; i + 1; i++] >
                    output >> i;
                <
            """);

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.NonBooleanCondition));
    }

    [Fact]
    public void Analyze_WellFormedControlFlow_ReportsNoErrors()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sample temp = 80;
                if [temp > 90] >
                    output >> "critica";
                <
                else if [temp > 70 and temp <= 90] >
                    output >> "alta";
                <
                else >
                    output >> "normal";
                <
            """);

        Assert.Empty(SemanticTestHarness.ErrorsOf(diagnostics));
    }

    [Fact]
    public void Analyze_MixCaseOfAnotherType_ReportsIncompatibleOperation()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sample opcion = 1;
                mix opcion >
                    case "uno": >
                        output >> 1;
                    <
                <
            """);

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.IncompatibleOperation));
    }

    [Fact]
    public void Analyze_MixWithCompositeSelector_ReportsIncompatibleOperation()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sequence<sample> datos = [1, 2];
                mix datos >
                    case 1: >
                        output >> 1;
                    <
                <
            """);

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.IncompatibleOperation));
    }

    [Fact]
    public void Analyze_MixWithDuplicatedCaseValue_ReportsIncompatibleOperation()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sample opcion = 1;
                mix opcion >
                    case 1: >
                        output >> "a";
                    <
                    case 1: >
                        output >> "b";
                    <
                <
            """);

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.IncompatibleOperation));
    }

    [Fact]
    public void Analyze_WellFormedMix_ReportsNoErrors()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                const sample OPCION = 2;
                mix OPCION >
                    case 1: >
                        output >> "Uno";
                    <
                    case 2: >
                        output >> "Dos";
                    <
                    default: >
                        output >> "Otro";
                    <
                <
            """);

        Assert.Empty(SemanticTestHarness.ErrorsOf(diagnostics));
    }

    [Fact]
    public void Analyze_LoopOverAConstant_ReportsConstantWrite()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                const sample LIMITE = 3;
                loop [sample i = 0; i < LIMITE; LIMITE++] >
                    output >> i;
                <
            """);

        Assert.True(SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.ConstantWrite));
    }
}
