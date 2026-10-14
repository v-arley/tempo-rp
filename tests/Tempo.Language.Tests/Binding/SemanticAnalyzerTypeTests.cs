using Tempo.Language.Diagnostics;
using Xunit;

namespace Tempo.Language.Tests.Binding;

/// <summary>
/// Verificación de tipos: E003, E004, E010, E019 y W004 (§4.4).
/// </summary>

public sealed class SemanticAnalyzerTypeTests
{
    [Theory]
    [InlineData("sample x = 3.14;")]
    [InlineData("wave x = 3;")]
    [InlineData("phrase x = 5;")]
    [InlineData("signal x = 1;")]
    [InlineData("tone x = \"A\";")]
    [InlineData("sequence<sample> x = 5;")]
    public void Analyze_InitializerOfAnotherType_ReportsIncompatibleAssignment(string declaration)
    {
        IReadOnlyList<Diagnostic> diagnostics =
            SemanticTestHarness.AnalyzeSession($"    {declaration}");

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.IncompatibleAssignment));
    }

    [Fact]
    public void Analyze_AssignmentOfAnotherType_ReportsIncompatibleAssignment()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sample x = 1;
                x = 2.5;
                output >> x;
            """);

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.IncompatibleAssignment));
    }

    [Theory]
    [InlineData("sample x = 1 + 2.5;")]
    [InlineData("signal x = 'a' > 'b';")]
    [InlineData("signal x = \"a\" < \"b\";")]
    [InlineData("signal x = 1 and 2;")]
    [InlineData("sample x = not 5;")]
    public void Analyze_UndefinedOperatorCombination_ReportsIncompatibleOperation(
        string declaration)
    {
        IReadOnlyList<Diagnostic> diagnostics =
            SemanticTestHarness.AnalyzeSession($"    {declaration}");

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.IncompatibleOperation));
    }

    [Fact]
    public void Analyze_SilenceAssignedToPhrase_IsAccepted()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                phrase texto = silence;
                output >> texto;
            """);

        Assert.Empty(SemanticTestHarness.ErrorsOf(diagnostics));
    }

    [Fact]
    public void Analyze_SilenceAssignedToSample_ReportsInvalidSilenceUsage()
    {
        IReadOnlyList<Diagnostic> diagnostics =
            SemanticTestHarness.AnalyzeSession("    sample x = silence;");

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.InvalidSilenceUsage));
    }

    [Fact]
    public void Analyze_VariableDeclaredAsSilence_ReportsInvalidSilenceUsage()
    {
        IReadOnlyList<Diagnostic> diagnostics =
            SemanticTestHarness.AnalyzeSession("    silence x = silence;");

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.InvalidSilenceUsage));
    }

    [Fact]
    public void Analyze_MatrixWithRowsOfDifferentLength_ReportsRaggedMatrix()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                score<sample> matriz = [[1, 2, 3], [4, 5]];
                output >> matriz[0][0];
            """);

        Assert.True(SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.RaggedMatrix));
    }

    [Fact]
    public void Analyze_MatrixWithUniformRows_IsAccepted()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                score<sample> matriz = [[1, 2, 3], [4, 5, 6]];
                output >> matriz[1][2];
            """);

        Assert.Empty(SemanticTestHarness.ErrorsOf(diagnostics));
    }

    [Fact]
    public void Analyze_CollectionWithMixedElementTypes_ReportsIncompatibleAssignment()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sequence<sample> datos = [1, 2.5, 3];
                output >> datos[0];
            """);

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.IncompatibleAssignment));
    }

    [Fact]
    public void Analyze_IndexOfWrongType_ReportsIncompatibleOperation()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sequence<sample> datos = [1, 2, 3];
                output >> datos["primero"];
            """);

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.IncompatibleOperation));
    }

    [Fact]
    public void Analyze_IndexingANonCompositeValue_ReportsIncompatibleOperation()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sample x = 1;
                output >> x[0];
            """);

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.IncompatibleOperation));
    }

    [Fact]
    public void Analyze_ScoreIndexedWithASingleIndex_ReportsIncompatibleOperation()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                score<sample> matriz = [[1, 2], [3, 4]];
                output >> matriz[0];
            """);

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.IncompatibleOperation));
    }

    [Fact]
    public void Analyze_SequenceIndexedWithTwoIndexes_ReportsIncompatibleOperation()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sequence<sample> datos = [1, 2, 3];
                output >> datos[0][1];
            """);

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.IncompatibleOperation));
    }

    [Fact]
    public void Analyze_LengthOfANonComposite_ReportsIncompatibleOperation()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                phrase texto = "abc";
                output >> texto.length();
            """);

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.IncompatibleOperation));
    }

    [Fact]
    public void Analyze_LengthOfASequence_ReturnsSample()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sequence<tone> notas = ['C', 'E'];
                sample cantidad = notas.length();
                output >> cantidad;
            """);

        Assert.Empty(SemanticTestHarness.ErrorsOf(diagnostics));
    }

    [Fact]
    public void Analyze_ComparingTwoSequences_ReportsIncompatibleOperation()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sequence<sample> a = [1, 2];
                sequence<sample> b = [3, 4];
                signal iguales = a == b;
                output >> iguales;
            """);

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.IncompatibleOperation));
    }

    [Fact]
    public void Analyze_OutputOfAComposite_ReportsIncompatibleOperation()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sequence<sample> datos = [1, 2];
                output >> datos;
            """);

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.IncompatibleOperation));
    }

    [Fact]
    public void Analyze_PhraseConcatenatedWithSample_ReportsImplicitConversion()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sample total = 13;
                output >> "Suma: " + total;
            """);

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.ImplicitPhraseConversion));

        Assert.Empty(SemanticTestHarness.ErrorsOf(diagnostics));
    }

    [Fact]
    public void Analyze_TwoPhrasesConcatenated_DoesNotReportImplicitConversion()
    {
        IReadOnlyList<Diagnostic> diagnostics =
            SemanticTestHarness.AnalyzeSession("    output >> \"Hola, \" + \"Mundo\";");

        Assert.False(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.ImplicitPhraseConversion));
    }

    [Fact]
    public void Analyze_IncrementOnANonSample_ReportsIncompatibleOperation()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                wave x = 1.0;
                x++;
                output >> x;
            """);

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.IncompatibleOperation));
    }

    [Fact]
    public void Analyze_InputIntoAComposite_ReportsIncompatibleOperation()
    {
        IReadOnlyList<Diagnostic> diagnostics = SemanticTestHarness.AnalyzeSession(
            """
                sequence<sample> datos = [1, 2];
                input << datos;
                output >> datos[0];
            """);

        Assert.True(
            SemanticTestHarness.HasCode(diagnostics, DiagnosticCodes.IncompatibleOperation));
    }
}
