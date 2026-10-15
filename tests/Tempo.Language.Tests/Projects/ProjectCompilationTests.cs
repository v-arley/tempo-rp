using Tempo.Language.Diagnostics;
using Xunit;

namespace Tempo.Language.Tests.Projects;

/// <summary>
/// Compilación y ejecución de proyectos multiarchivo: pruebas canónicas
/// §10.4 (I1-I3) y §10.5 (C1), y reglas de proyecto de §6.5 y §6.6.
/// </summary>

public sealed class ProjectCompilationTests
{
    private const string Doubler =
        "sample : compose doble(sample x) >\n" +
        "    ret x * 2;\n" +
        "<\n";

    [Fact]
    public void CanonicalI1_ImportedFunction_PrintsTen()
    {
        using var project = ProjectWorkspace.With(
            ("utilidades.tm", Doubler),
            ("principal.tmpr", "import \"utilidades.tm\";\nsession > output >> doble(5); <\n"));

        Assert.Equal("10", project.RunDirectory());
    }

    [Fact]
    public void CanonicalI2_FunctionMissingFromImport_ReportsE001()
    {
        using var project = ProjectWorkspace.With(
            ("utilidades.tm", Doubler),
            ("principal.tmpr", "import \"utilidades.tm\";\nsession > output >> triple(5); <\n"));

        Compilation compilation = project.CompileDirectory();

        AssertReports(compilation, DiagnosticCodes.UndeclaredIdentifier, "principal.tmpr");
    }

    [Fact]
    public void CanonicalI3_IncompatibleArgumentForImportedFunction_ReportsE008()
    {
        using var project = ProjectWorkspace.With(
            ("utilidades.tm", Doubler),
            ("principal.tmpr", "import \"utilidades.tm\";\nsession > output >> doble(\"cinco\"); <\n"));

        Compilation compilation = project.CompileDirectory();

        AssertReports(compilation, DiagnosticCodes.IncompatibleArguments, "principal.tmpr");
    }

    [Fact]
    public void CanonicalC1_CircularImport_ReportsE012()
    {
        using var project = ProjectWorkspace.With(
            ("a.tm", "import \"b.tm\";\n"),
            ("b.tm", "import \"a.tm\";\n"),
            ("principal.tmpr", "import \"a.tm\";\nsession > output >> 1; <\n"));

        Compilation compilation = project.CompileDirectory();

        Diagnostic cycle = Assert.Single(
            compilation.Diagnostics,
            diagnostic => diagnostic.Code == DiagnosticCodes.ImportCycle);

        Assert.Contains("a.tm", cycle.Description, StringComparison.Ordinal);
        Assert.Contains("b.tm", cycle.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void Import_FileThatDoesNotExist_ReportsE014()
    {
        using var project = ProjectWorkspace.With(
            ("principal.tmpr", "import \"falta.tm\";\nsession > output >> 1; <\n"));

        Compilation compilation = project.CompileDirectory();

        AssertReports(compilation, DiagnosticCodes.ImportNotFound, "principal.tmpr");
    }

    [Fact]
    public void Import_IsTextual_NestedImportIsVisibleFromProjectFile()
    {
        using var project = ProjectWorkspace.With(
            ("base.tm", Doubler),
            ("intermedio.tm", "import \"base.tm\";\n"),
            ("principal.tmpr", "import \"intermedio.tm\";\nsession > output >> doble(21); <\n"));

        Assert.Equal("42", project.RunDirectory());
    }

    [Fact]
    public void Module_WithSession_ReportsE015()
    {
        using var project = ProjectWorkspace.With(
            ("modulo.tm", "session > output >> 1; <\n"),
            ("principal.tmpr", "session > output >> 2; <\n"));

        Compilation compilation = project.CompileDirectory();

        AssertReports(compilation, DiagnosticCodes.ModuleWithSession, "modulo.tm");
    }

    [Fact]
    public void Directory_WithoutProjectFile_ReportsE016()
    {
        using var project = ProjectWorkspace.With(
            ("utilidades.tm", Doubler));

        Compilation compilation = project.CompileDirectory();

        Assert.Contains(
            compilation.Diagnostics,
            diagnostic => diagnostic.Code == DiagnosticCodes.MissingProjectFile);
    }

    [Fact]
    public void Directory_WithTwoProjectFiles_ReportsE017()
    {
        using var project = ProjectWorkspace.With(
            ("a.tmpr", "session > output >> 1; <\n"),
            ("b.tmpr", "session > output >> 2; <\n"));

        Compilation compilation = project.CompileDirectory();

        AssertReports(compilation, DiagnosticCodes.MultipleProjectFiles, "b.tmpr");
    }

    [Fact]
    public void File_OpenedAlone_IgnoresOtherProjectFilesInDirectory()
    {
        using var project = ProjectWorkspace.With(
            ("a.tmpr", "session > output >> 1; <\n"),
            ("b.tmpr", "session > output >> 2; <\n"));

        Compilation compilation = project.CompileFile("a.tmpr");

        Assert.False(compilation.HasErrors);
        Assert.Single(compilation.Project.Files);
    }

    [Fact]
    public void Call_ToFunctionOfUnimportedModule_ReportsE020()
    {
        using var project = ProjectWorkspace.With(
            ("utilidades.tm", Doubler),
            ("principal.tmpr", "session > output >> doble(5); <\n"));

        Compilation compilation = project.CompileDirectory();

        AssertReports(compilation, DiagnosticCodes.UnimportedFunction, "principal.tmpr");
    }

    [Fact]
    public void Function_DeclaredInTwoFiles_ReportsE013InTheImportingFile()
    {
        using var project = ProjectWorkspace.With(
            ("utilidades.tm", Doubler),
            ("principal.tmpr", "import \"utilidades.tm\";\n" + Doubler + "session > output >> doble(5); <\n"));

        Compilation compilation = project.CompileDirectory();

        AssertReports(compilation, DiagnosticCodes.FunctionOverload, "principal.tmpr");
    }

    [Fact]
    public void ModuleFunction_InvokedFromProjectFile_IsNotReportedAsUnused()
    {
        using var project = ProjectWorkspace.With(
            ("utilidades.tm", Doubler),
            ("principal.tmpr", "import \"utilidades.tm\";\nsession > output >> doble(5); <\n"));

        Compilation compilation = project.CompileDirectory();

        Assert.DoesNotContain(
            compilation.Diagnostics,
            diagnostic => diagnostic.Code == DiagnosticCodes.UnusedFunction);
    }

    [Fact]
    public void ModuleFunction_NeverInvoked_ReportsW003InTheModule()
    {
        using var project = ProjectWorkspace.With(
            ("utilidades.tm", Doubler),
            ("principal.tmpr", "import \"utilidades.tm\";\nsession > output >> 1; <\n"));

        Compilation compilation = project.CompileDirectory();

        AssertReports(compilation, DiagnosticCodes.UnusedFunction, "utilidades.tm");
    }

    [Fact]
    public void SyntaxError_InModule_DoesNotHideErrorsOfOtherFiles()
    {
        using var project = ProjectWorkspace.With(
            ("roto.tm", "sample : compose roto( > ret 1; <\n"),
            ("principal.tmpr", "session > output >> y; <\n"));

        Compilation compilation = project.CompileDirectory();

        AssertReports(compilation, DiagnosticCodes.LexicalOrSyntaxError, "roto.tm");
        AssertReports(compilation, DiagnosticCodes.UndeclaredIdentifier, "principal.tmpr");
    }

    private static void AssertReports(Compilation compilation, string code, string fileName)
    {
        Assert.Contains(
            compilation.Diagnostics,
            diagnostic => diagnostic.Code == code && diagnostic.FileName == fileName);
    }
}
