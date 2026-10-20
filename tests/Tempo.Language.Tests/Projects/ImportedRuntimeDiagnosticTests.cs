using Tempo.Language.Evaluation;
using Xunit;

namespace Tempo.Language.Tests.Projects;

public sealed class ImportedRuntimeDiagnosticTests
{
    [Fact]
    public void RuntimeError_InImportedModule_ReportsCorrectFileAndLine()
    {
        using var project = ProjectWorkspace.With(
            (
                "matematicas.tm",
                "sample : compose dividir(sample numero) >\n" +
                "    ret numero / 0;\n" +
                "<\n"
            ),
            (
                "principal.tmpr",
                "import \"matematicas.tm\";\n" +
                "session >\n" +
                "    output >> dividir(10);\n" +
                "<\n"
            )
        );

        TmRuntimeException exception =
            Assert.Throws<TmRuntimeException>(
                () => project.RunDirectory());

        Assert.Equal(
            RuntimeErrorCodes.DivisionByZero,
            exception.Code);

        Assert.Equal(
            "matematicas.tm",
            exception.FileName);

        Assert.Equal(
            2,
            exception.Line);

        Assert.Equal(
            "matematicas.tm",
            exception.ToDiagnostic("principal.tmpr").FileName);
    }
}