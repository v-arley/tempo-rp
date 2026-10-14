using Tempo.Language.Diagnostics;
using Xunit;

namespace Tempo.Language.Tests.Binding;

/// <summary>
/// Las cinco pruebas semánticas canónicas de la §10.3 de la especificación
/// TM v1.1. Cada una exige que el código esperado sea el único error
/// reportado, lo que verifica de paso que un error no arrastra derivados.
/// </summary>

public sealed class SemanticAnalyzerCanonicalTests
{
    [Fact]
    public void Analyze_SampleInitializedWithFloatLiteral_ReportsIncompatibleAssignment()
    {
        // M1 — Tipo incompatible en asignación.
        IReadOnlyList<Diagnostic> diagnostics =
            SemanticTestHarness.AnalyzeSession("    sample x = 3.14;");

        IReadOnlyList<Diagnostic> errors = SemanticTestHarness.ErrorsOf(diagnostics);

        Assert.Single(errors);
        Assert.Equal(DiagnosticCodes.IncompatibleAssignment, errors[0].Code);
        Assert.Contains("'wave'", errors[0].Description, StringComparison.Ordinal);
        Assert.Contains("'sample'", errors[0].Description, StringComparison.Ordinal);
    }

    [Fact]
    public void Analyze_NonSignalIfCondition_ReportsNonBooleanCondition()
    {
        // M2 — Condición no booleana.
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sample x = 5;
                if [x + 1] >
                    output >> 1;
                <
            """);

        IReadOnlyList<Diagnostic> errors = SemanticTestHarness.ErrorsOf(diagnostics);

        Assert.Single(errors);
        Assert.Equal(DiagnosticCodes.NonBooleanCondition, errors[0].Code);
    }

    [Fact]
    public void Analyze_UndeclaredIdentifierInOutput_ReportsUndeclaredIdentifier()
    {
        // M3 — Variable no declarada.
        IReadOnlyList<Diagnostic> diagnostics =
            SemanticTestHarness.AnalyzeSession("    output >> y;");

        IReadOnlyList<Diagnostic> errors = SemanticTestHarness.ErrorsOf(diagnostics);

        Assert.Single(errors);
        Assert.Equal(DiagnosticCodes.UndeclaredIdentifier, errors[0].Code);
        Assert.Contains("'y'", errors[0].Description, StringComparison.Ordinal);
    }

    [Fact]
    public void Analyze_DuplicatedFunctionName_ReportsFunctionOverload()
    {
        // M4 — Redeclaración de función.
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.Analyze(
            """
            sample : compose sumar(sample a, sample b) >
                ret a + b;
            <

            sample : compose sumar(sample a) >
                ret a;
            <

            session >
                output >> sumar(1, 2);
            <
            """);

        IReadOnlyList<Diagnostic> errors = SemanticTestHarness.ErrorsOf(diagnostics);

        Assert.Single(errors);
        Assert.Equal(DiagnosticCodes.FunctionOverload, errors[0].Code);
    }

    [Fact]
    public void Analyze_AssignmentToConstant_ReportsConstantWrite()
    {
        // M5 — Modificación de constante.
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                const sample X = 10;
                X = 20;
            """);

        IReadOnlyList<Diagnostic> errors = SemanticTestHarness.ErrorsOf(diagnostics);

        Assert.Single(errors);
        Assert.Equal(DiagnosticCodes.ConstantWrite, errors[0].Code);
        Assert.Contains("'X'", errors[0].Description, StringComparison.Ordinal);
    }
}
