using Tempo.Language.Diagnostics;
using Xunit;

namespace Tempo.Language.Tests.Binding;

public sealed class CanonicalS5Tests
{
    [Fact]
    public void S5_RaggedMatrix_ReportsE019OnCorrectLine()
    {
        const string source =
            """
            session >
                score<sample> m = [
                    [1, 2],
                    [3]
                ];
            <
            """;

        IReadOnlyList<Diagnostic> diagnostics =
            SemanticTestHarness.Analyze(source);

        IReadOnlyList<Diagnostic> errors =
            SemanticTestHarness.ErrorsOf(diagnostics);

        Diagnostic error = Assert.Single(errors);

        Assert.Equal(
            DiagnosticCodes.RaggedMatrix,
            error.Code);

        Assert.Equal(4, error.Line);
    }
}