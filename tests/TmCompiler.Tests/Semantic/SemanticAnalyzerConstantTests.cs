using TmCompiler.Domain.Diagnostics;
using Xunit;

namespace TmCompiler.Tests.Semantic;

/// <summary>
/// Constantes: inmutabilidad (E009, §4.3) e inicializador constante
/// (E018, §5.2).
/// </summary>

public sealed class SemanticAnalyzerConstantTests
{
    [Fact]
    public void Analyze_IncrementOfAConstant_ReportsConstantWrite()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                const sample X = 10;
                X++;
            """);

        Assert.True(SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.ConstantWrite));
    }

    [Fact]
    public void Analyze_InputIntoAConstant_ReportsConstantWrite()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                const sample X = 10;
                input << X;
            """);

        Assert.True(SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.ConstantWrite));
    }

    [Fact]
    public void Analyze_IndexedAssignmentToAConstant_ReportsConstantWrite()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                const sequence<sample> DATOS = [1, 2, 3];
                DATOS[0] = 9;
            """);

        Assert.True(SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.ConstantWrite));
    }

    [Fact]
    public void Analyze_ReadingAConstant_IsAccepted()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                const sample LIMITE = 100;
                output >> LIMITE;
            """);

        Assert.Empty(SemanticTestHarness.ErrorsOf(diagnostics));
    }

    [Fact]
    public void Analyze_ConstantInitializedWithAVariable_ReportsNonConstantInitializer()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sample x = 10;
                const sample Y = x + 5;
                output >> Y;
            """);

        Assert.True(
            SemanticTestHarness.HasCode(
                diagnostics,
                DiagnosticCodes.NonConstantConstantInitializer));
    }

    [Fact]
    public void Analyze_ConstantInitializedWithLiteralArithmetic_IsAccepted()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                const sample LIMITE = 10 * 10;
                output >> LIMITE;
            """);

        Assert.Empty(SemanticTestHarness.ErrorsOf(diagnostics));
    }

    [Fact]
    public void Analyze_ConstantInitializedWithACall_ReportsNonConstantInitializer()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.Analyze(
            """
            sample : compose calcular() >
                ret 5;
            <

            session >
                const sample X = calcular();
                output >> X;
            <
            """);

        Assert.True(
            SemanticTestHarness.HasCode(
                diagnostics,
                DiagnosticCodes.NonConstantConstantInitializer));
    }

    [Fact]
    public void Analyze_ConstantOfTheWrongType_ReportsIncompatibleAssignment()
    {
        // La comprobación de tipo se hace aunque el inicializador sea constante.
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                const sample X = 3.14;
                output >> X;
            """);

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.IncompatibleAssignment));
    }
}
