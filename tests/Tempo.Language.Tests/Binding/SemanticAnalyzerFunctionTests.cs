using Tempo.Language.Diagnostics;
using Xunit;

namespace Tempo.Language.Tests.Binding;

/// <summary>
/// Funciones: retornos (E006), llamadas (E007, E008), sobrecarga (E013) y
/// funciones nunca invocadas (W003), según la §5.3.
/// </summary>

public sealed class SemanticAnalyzerFunctionTests
{
    [Fact]
    public void Analyze_NonVoidFunctionWithoutReturnOnEveryPath_ReportsInvalidReturn()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.Analyze(
            """
            sample : compose absoluto(sample n) >
                if [n > 0] >
                    ret n;
                <
            <

            session >
                output >> absoluto(5);
            <
            """);

        Assert.True(SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.InvalidReturn));
    }

    [Fact]
    public void Analyze_NonVoidFunctionReturningOnEveryPath_ReportsNoErrors()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.Analyze(
            """
            sample : compose absoluto(sample n) >
                if [n > 0] >
                    ret n;
                <
                ret 0 - n;
            <

            session >
                output >> absoluto(5);
            <
            """);

        Assert.Empty(SemanticTestHarness.ErrorsOf(diagnostics));
    }

    [Fact]
    public void Analyze_VoidFunctionReturningAValue_ReportsInvalidReturn()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.Analyze(
            """
            void : compose saludar(phrase nombre) >
                ret nombre;
            <

            session >
                saludar("TM");
            <
            """);

        Assert.True(SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.InvalidReturn));
    }

    [Fact]
    public void Analyze_ReturnOfAnotherType_ReportsIncompatibleAssignment()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.Analyze(
            """
            sample : compose valor() >
                ret 3.5;
            <

            session >
                output >> valor();
            <
            """);

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.IncompatibleAssignment));
    }

    [Fact]
    public void Analyze_SessionReturningAValue_ReportsInvalidReturn()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession("    ret 1;");

        Assert.True(SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.InvalidReturn));
    }

    [Fact]
    public void Analyze_CallWithWrongArgumentCount_ReportsWrongArgumentCount()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.Analyze(
            """
            sample : compose sumar(sample a, sample b) >
                ret a + b;
            <

            session >
                output >> sumar(1);
            <
            """);

        Assert.True(SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.WrongArgumentCount));
    }

    [Fact]
    public void Analyze_CallWithArgumentOfAnotherType_ReportsIncompatibleArguments()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.Analyze(
            """
            sample : compose doble(sample x) >
                ret x * 2;
            <

            session >
                output >> doble("cinco");
            <
            """);

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.IncompatibleArguments));
    }

    [Fact]
    public void Analyze_CallToUndeclaredFunction_ReportsUndeclaredIdentifier()
    {
        IReadOnlyList<Diagnostic> diagnostics =
            SemanticTestHarness.AnalyzeSession("    output >> triple(5);");

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.UndeclaredIdentifier));
    }

    [Fact]
    public void Analyze_CallBeforeDeclaration_IsAcceptedThanksToTheFirstPass()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.Analyze(
            """
            sample : compose primera(sample n) >
                ret segunda(n);
            <

            sample : compose segunda(sample n) >
                ret n * 2;
            <

            session >
                output >> primera(4);
            <
            """);

        Assert.Empty(SemanticTestHarness.ErrorsOf(diagnostics));
    }

    [Fact]
    public void Analyze_RecursiveFunction_IsAccepted()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.Analyze(
            """
            sample : compose factorial(sample n) >
                if [n <= 1] >
                    ret 1;
                <
                ret n * factorial(n - 1);
            <

            session >
                output >> factorial(5);
            <
            """);

        Assert.Empty(SemanticTestHarness.ErrorsOf(diagnostics));
    }

    [Fact]
    public void Analyze_VoidCallInsideAnExpression_ReportsIncompatibleOperation()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.Analyze(
            """
            void : compose saludar() >
                ret;
            <

            session >
                sample x = saludar();
                output >> x;
            <
            """);

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.IncompatibleOperation));
    }

    [Fact]
    public void Analyze_VoidCallAsStatement_IsAccepted()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.Analyze(
            """
            void : compose saludar(phrase nombre) >
                output >> "Hola, " + nombre;
                ret;
            <

            session >
                saludar("TM");
            <
            """);

        Assert.Empty(SemanticTestHarness.ErrorsOf(diagnostics));
    }

    [Fact]
    public void Analyze_FunctionNeverInvoked_ReportsUnusedFunction()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.Analyze(
            """
            sample : compose olvidada(sample n) >
                ret n;
            <

            session >
                output >> 1;
            <
            """);

        Assert.True(SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.UnusedFunction));
    }

    [Fact]
    public void Analyze_DuplicatedParameterName_ReportsRedeclaration()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.Analyze(
            """
            sample : compose sumar(sample a, sample a) >
                ret a;
            <

            session >
                output >> sumar(1, 2);
            <
            """);

        Assert.True(SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.Redeclaration));
    }

    [Fact]
    public void Analyze_ProjectFileWithoutSession_ReportsMissingSession()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.Analyze(
            """
            sample : compose doble(sample x) >
                ret x * 2;
            <
            """,
            "principal.tmpr");

        Assert.True(
            SemanticTestHarness.HasCode(
                diagnostics,
                DiagnosticCodes.SessionMissingOrDuplicated));
    }

    [Fact]
    public void Analyze_ModuleWithoutSession_DoesNotReportMissingSession()
    {
        // Un módulo .tm no necesita punto de entrada.
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.Analyze(
            """
            sample : compose doble(sample x) >
                ret x * 2;
            <
            """,
            "matematicas.tm");

        Assert.False(
            SemanticTestHarness.HasCode(
                diagnostics,
                DiagnosticCodes.SessionMissingOrDuplicated));
    }
}
