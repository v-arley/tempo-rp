using Tempo.Language.Binding;
using Tempo.Language.Diagnostics;
using Tempo.Language.Evaluation;
using Xunit;

namespace Tempo.Language.Tests.Projects;

/// <summary>
/// La innovación a nivel de proyecto: dónde puede declararse
/// <c>tempo</c> (E041), el ritmo que expone la compilación y la ejecución de
/// <c>pulse</c> desde la CLI.
/// </summary>

public sealed class TempoProjectTests
{
    private const string Arpeggio =
        "tempo 96;\n" +
        "\n" +
        "void : compose acorde(sample raiz) >\n" +
        "    pulse [raiz];\n" +
        "    pulse [raiz + 4];\n" +
        "    pulse [raiz + 7];\n" +
        "    ret;\n" +
        "<\n" +
        "\n" +
        "session >\n" +
        "    sequence<sample> notas = [40, 55, 70];\n" +
        "    loop [sample i = 0; i < notas.length(); i++] >\n" +
        "        acorde(notas[i]);\n" +
        "    <\n" +
        "<\n";

    [Fact]
    public void CanonicalArpeggio_CompilesWithTempo96AndRunsWithoutOutput()
    {
        using var project = ProjectWorkspace.With(("arpegio.tmpr", Arpeggio));

        Compilation compilation = project.CompileDirectory();

        Assert.Empty(compilation.Diagnostics);
        Assert.Equal(96, compilation.TempoBpm);
        Assert.Equal(string.Empty, project.RunDirectory());
    }

    [Fact]
    public void CanonicalArpeggio_EmitsThreeBurstsPerNote()
    {
        using var project = ProjectWorkspace.With(("arpegio.tmpr", Arpeggio));
        var pulse = new RecordedPulse();

        project.RunDirectory(pulse);

        Assert.Equal(new double[] { 40, 44, 47, 55, 59, 62, 70, 74, 77 }, pulse.Intensities);
    }

    [Theory]
    [InlineData("pulse [50];", 50.0)]
    [InlineData("pulse [12.5];", 12.5)]
    [InlineData("pulse [150];", 100.0)]
    [InlineData("pulse [100.5];", 100.0)]
    public void Pulse_PositiveIntensity_IsEmittedWithinLimit(string statement, double expected)
    {
        using var project = ProjectWorkspace.With(("principal.tmpr", $"session >\n    {statement}\n<\n"));
        var pulse = new RecordedPulse();

        project.RunDirectory(pulse);

        Assert.Equal(new[] { expected }, pulse.Intensities);
    }

    [Theory]
    [InlineData("pulse [0];")]
    [InlineData("pulse [0.0];")]
    [InlineData("pulse [-5];")]
    [InlineData("pulse [-0.5];")]
    public void Pulse_ZeroOrNegativeIntensity_EmitsNothing(string statement)
    {
        using var project = ProjectWorkspace.With(("principal.tmpr", $"session >\n    {statement}\n<\n"));
        var pulse = new RecordedPulse();

        project.RunDirectory(pulse);

        Assert.Empty(pulse.Intensities);
    }

    [Fact]
    public void Pulse_DoesNotWriteToOutput()
    {
        using var project = ProjectWorkspace.With(
            ("principal.tmpr", "session >\n    pulse [50];\n    output >> 1;\n<\n"));

        Assert.Equal("1", project.RunDirectory(new RecordedPulse()));
    }

    [Fact]
    public void ProjectWithoutTempo_UsesDefault()
    {
        using var project = ProjectWorkspace.With(("principal.tmpr", "session > output >> 1; <\n"));

        Assert.Equal(TempoLimits.DefaultBpm, project.CompileDirectory().TempoBpm);
    }

    [Fact]
    public void TempoOutOfRange_UsesDefault()
    {
        using var project = ProjectWorkspace.With(("principal.tmpr", "tempo 500;\nsession > <\n"));

        Compilation compilation = project.CompileDirectory();

        Assert.Contains(compilation.Diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.TempoOutOfRange);
        Assert.Equal(TempoLimits.DefaultBpm, compilation.TempoBpm);
    }

    [Fact]
    public void CanonicalTempoInModule_ReportsE041InModule()
    {
        using var project = ProjectWorkspace.With(
            ("utilidades.tm", "tempo 120;\n"),
            ("principal.tmpr", "session > <\n"));

        Diagnostic error = Assert.Single(
            project.CompileDirectory().Diagnostics,
            diagnostic => diagnostic.Code == DiagnosticCodes.TempoNotAllowed);

        Assert.Equal("utilidades.tm", error.FileName);
        Assert.Equal(1, error.Line);
    }

    [Fact]
    public void SecondTempoInProjectFile_ReportsE041OnDuplicate()
    {
        using var project = ProjectWorkspace.With(
            ("principal.tmpr", "tempo 96;\ntempo 120;\nsession > <\n"));

        Compilation compilation = project.CompileDirectory();

        Diagnostic error = Assert.Single(
            compilation.Diagnostics,
            diagnostic => diagnostic.Code == DiagnosticCodes.TempoNotAllowed);

        Assert.Equal(2, error.Line);
        Assert.Equal(96, compilation.TempoBpm);
    }

    [Fact]
    public void Pulse_DivisionByZero_ReportsRuntimeError()
    {
        using var project = ProjectWorkspace.With(
            ("principal.tmpr", "session >\n    sample cero = 0;\n    pulse [10 / cero];\n<\n"));

        TmRuntimeException error = Assert.Throws<TmRuntimeException>(() => project.RunDirectory());

        Assert.Equal(RuntimeErrorCodes.DivisionByZero, error.Code);
        Assert.Equal(3, error.Line);
    }

    private sealed class RecordedPulse : IRuntimePulse
    {
        public List<double> Intensities { get; } = new();

        public void Emit(double intensity)
        {
            Intensities.Add(intensity);
        }
    }
}
