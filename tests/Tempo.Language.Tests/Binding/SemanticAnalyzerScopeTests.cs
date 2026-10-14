using Tempo.Language.Diagnostics;
using Xunit;

namespace Tempo.Language.Tests.Binding;

/// <summary>
/// Reglas de ámbito y declaración: E001, E002, W001 y W002 (§5.1 y §5.2).
/// </summary>

public sealed class SemanticAnalyzerScopeTests
{
    [Fact]
    public void Analyze_AssignmentToUndeclaredIdentifier_ReportsUndeclaredIdentifier()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession("    x = 5;");

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.UndeclaredIdentifier));
    }

    [Fact]
    public void Analyze_IdentifierUsedManyTimes_ReportsUndeclaredIdentifierOnce()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                output >> y;
                output >> y;
                output >> y;
            """);

        Assert.Single(SemanticTestHarness.ErrorsOf(diagnostics));
    }

    [Fact]
    public void Analyze_RedeclarationInSameScope_ReportsRedeclaration()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sample x = 1;
                sample x = 2;
                output >> x;
            """);

        Assert.Equal(
            DiagnosticCodes.Redeclaration,
            SemanticTestHarness.Single(diagnostics, DiagnosticCodes.Redeclaration).Code);
    }

    [Fact]
    public void Analyze_DeclarationInInnerBlock_ReportsShadowing()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sample x = 1;
                if [x > 0] >
                    sample x = 2;
                    output >> x;
                <
            """);

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.ShadowedIdentifier));

        Assert.Empty(SemanticTestHarness.ErrorsOf(diagnostics));
    }

    [Fact]
    public void Analyze_VariableDeclaredInInnerBlock_IsNotVisibleOutside()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sample x = 1;
                if [x > 0] >
                    sample interna = 2;
                    output >> interna;
                <
                output >> interna;
            """);

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.UndeclaredIdentifier));
    }

    [Fact]
    public void Analyze_DeclaredButNeverRead_ReportsUnusedVariable()
    {
        IReadOnlyList<Diagnostic> diagnostics =
            SemanticTestHarness.AnalyzeSession("    sample x = 1;");

        Assert.True(SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.UnusedVariable));
    }

    [Fact]
    public void Analyze_OnlyAssignedNeverRead_ReportsUnusedVariable()
    {
        // Escribir en una variable no cuenta como usarla.
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sample x = 1;
                x = 2;
            """);

        Assert.True(SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.UnusedVariable));
    }

    [Fact]
    public void Analyze_DeclaredAndRead_DoesNotReportUnusedVariable()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sample x = 1;
                output >> x;
            """);

        Assert.False(SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.UnusedVariable));
    }

    [Fact]
    public void Analyze_UnusedParameter_DoesNotReportUnusedVariable()
    {
        // Los parámetros forman parte de la firma pública de la función.
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.Analyze(
            """
            sample : compose constante(sample ignorado) >
                ret 1;
            <

            session >
                output >> constante(5);
            <
            """);

        Assert.False(SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.UnusedVariable));
    }

    [Fact]
    public void Analyze_LoopVariableRedeclaredInBody_ReportsRedeclaration()
    {
        // El inicializador del loop y su cuerpo comparten ámbito (§5.1.4).
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                loop [sample i = 0; i < 3; i++] >
                    sample i = 9;
                    output >> i;
                <
            """);

        Assert.True(SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.Redeclaration));
    }

    [Fact]
    public void Analyze_LoopVariableUsedAfterLoop_ReportsUndeclaredIdentifier()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                loop [sample i = 0; i < 3; i++] >
                    output >> i;
                <
                output >> i;
            """);

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.UndeclaredIdentifier));
    }
}
