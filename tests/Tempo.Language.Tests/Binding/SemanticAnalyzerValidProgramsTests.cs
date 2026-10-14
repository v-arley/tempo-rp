using Tempo.Language.Diagnostics;
using Xunit;

namespace Tempo.Language.Tests.Binding;

/// <summary>
/// Programas correctos de la §8 y §10.1 de la especificación (no deben
/// producir errores) y comprobaciones del modelo acumulativo de diagnósticos.
/// </summary>

public sealed class SemanticAnalyzerValidProgramsTests
{
    [Fact]
    public void Analyze_HelloWorld_ReportsNoErrors()
    {
        IReadOnlyList<Diagnostic> diagnostics =
            SemanticTestHarness.AnalyzeSession("    output >> \"Hola, Mundo\";");

        Assert.Empty(SemanticTestHarness.ErrorsOf(diagnostics));
    }

    [Fact]
    public void Analyze_BasicArithmetic_ReportsNoErrors()
    {
        // Prueba V1 de la §10.1.
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sample a = 10;
                sample b = 3;
                output >> "Suma: " + (a + b);
                output >> "Resta: " + (a - b);
                output >> "Mult: " + (a * b);
                output >> "Div: " + (a / b);
            """);

        Assert.Empty(SemanticTestHarness.ErrorsOf(diagnostics));
    }

    [Fact]
    public void Analyze_NestedConditionals_ReportsNoErrors()
    {
        // Prueba V2 de la §10.1.
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sample valor = 15;
                if [valor > 10] >
                    if [valor < 20] >
                        output >> "Entre 10 y 20";
                    <
                <
            """);

        Assert.Empty(SemanticTestHarness.ErrorsOf(diagnostics));
    }

    [Fact]
    public void Analyze_RecursiveFibonacci_ReportsNoErrors()
    {
        // Prueba V3 de la §10.1.
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.Analyze(
            """
            sample : compose fibonacci(sample n) >
                if [n <= 1] >
                    ret n;
                <
                ret fibonacci(n - 1) + fibonacci(n - 2);
            <

            session >
                output >> "Fib(7) = " + fibonacci(7);
            <
            """);

        Assert.Empty(SemanticTestHarness.ErrorsOf(diagnostics));
    }

    [Fact]
    public void Analyze_SequenceManipulation_ReportsNoErrors()
    {
        // Prueba V4 de la §10.1.
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sequence<sample> datos = [10, 20, 30];
                output >> datos[0];
                datos[1] = 40;
                output >> datos[1];
                sample total = datos.length();
                output >> total;
            """);

        Assert.Empty(SemanticTestHarness.ErrorsOf(diagnostics));
    }

    [Fact]
    public void Analyze_ConstantsAndMixedTypes_ReportsNoErrors()
    {
        // Prueba V5 de la §10.1: dispara W004, pero ningún error.
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                const sample LIMITE = 100;
                const phrase ETIQUETA = "El limite es: ";
                output >> ETIQUETA + LIMITE;
            """);

        Assert.Empty(SemanticTestHarness.ErrorsOf(diagnostics));

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.ImplicitPhraseConversion));
    }

    [Fact]
    public void Analyze_LoopsAndRepeat_ReportsNoErrors()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                loop [sample i = 0; i < 5; i++] >
                    output >> i;
                <

                sample contador = 0;
                repeat [contador < 5] >
                    output >> contador;
                    contador++;
                <
            """);

        Assert.Empty(SemanticTestHarness.ErrorsOf(diagnostics));
    }

    [Fact]
    public void Analyze_MatrixOperations_ReportsNoErrors()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sequence<tone> notas = ['C', 'E', 'G'];
                output >> notas[0];
                notas[1] = 'D';

                score<sample> matriz = [[1, 2, 3], [4, 5, 6]];
                output >> matriz[1][2];
                matriz[0][1] = 9;
            """);

        Assert.Empty(SemanticTestHarness.ErrorsOf(diagnostics));
    }

    [Fact]
    public void Analyze_ProgramWithSeveralIndependentErrors_ReportsAllOfThem()
    {
        // El analizador acumula: no aborta en el primer error.
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sample a = 3.14;
                const sample B = 10;
                B = 20;
                output >> noExiste;
            """);

        IReadOnlyList<Diagnostic> errors = SemanticTestHarness.ErrorsOf(diagnostics);

        Assert.Equal(3, errors.Count);
        Assert.Contains(errors, error => error.Code == DiagnosticCodes.IncompatibleAssignment);
        Assert.Contains(errors, error => error.Code == DiagnosticCodes.ConstantWrite);
        Assert.Contains(errors, error => error.Code == DiagnosticCodes.UndeclaredIdentifier);
    }

    [Fact]
    public void Analyze_ErrorInsideASubexpression_DoesNotReportDerivedErrors()
    {
        // El tipo desconocido se propaga en silencio: un solo diagnóstico.
        IReadOnlyList<Diagnostic> diagnostics =
            SemanticTestHarness.AnalyzeSession("    sample x = noExiste + 1;");

        IReadOnlyList<Diagnostic> errors = SemanticTestHarness.ErrorsOf(diagnostics);

        Assert.Single(errors);
        Assert.Equal(DiagnosticCodes.UndeclaredIdentifier, errors[0].Code);
    }

    [Fact]
    public void Analyze_ProgramWithErrors_DoesNotThrow()
    {
        Exception exception = Record.Exception(
            () => SemanticTestHarness.AnalyzeSession(
                """
                    sample a = 3.14;
                    b = a + "texto";
                    output >> c[0];
                """));

        Assert.Null(exception);
    }

    [Fact]
    public void Analyze_ModuleWithUnresolvedImports_DoesNotReportImportedCallsAsUndeclared()
    {
        // Sin las firmas del archivo importado no se puede afirmar que la
        // función no exista: se deja para el análisis de proyecto.
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.Analyze(
            """
            import "matematicas.tm";

            session >
                output >> doble(5);
            <
            """);

        Assert.False(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.UndeclaredIdentifier));
    }
}
