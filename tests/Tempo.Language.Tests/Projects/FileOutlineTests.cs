using Tempo.Language.Outline;
using Xunit;

namespace Tempo.Language.Tests.Projects;

/// <summary>
/// Resumen de un archivo para el autocompletado (<see cref="Compilation.OutlineOf"/>):
/// funciones visibles según las importaciones y nombres locales por línea.
/// </summary>

public sealed class FileOutlineTests
{
    private const string Module =
        "sample : compose doble(sample x) >\n" + // 1
        "    ret x * 2;\n" +                     // 2
        "<\n";                                   // 3

    private const string Program =
        "import \"utilidades.tm\";\n" +                   // 1
        "void : compose saludar(phrase nombre) >\n" +     // 2
        "    const sample VECES = 2;\n" +                 // 3
        "    output >> nombre;\n" +                       // 4
        "<\n" +                                           // 5
        "session >\n" +                                   // 6
        "    sample edad = doble(10);\n" +                // 7
        "    loop [sample i = 0; i < 3; i++] >\n" +       // 8
        "        if [i == 1] >\n" +                       // 9
        "            phrase texto = \"uno\";\n" +         // 10
        "        <\n" +                                   // 11
        "    <\n" +                                       // 12
        "    signal listo = true;\n" +                    // 13
        "<\n";                                            // 14

    private static FileOutline OutlineOfProgram(params (string FileName, string Source)[] extraFiles)
    {
        using var project = ProjectWorkspace.With([("utilidades.tm", Module), ("principal.tmpr", Program), .. extraFiles]);

        return project.CompileDirectory().OutlineOf(project.PathOf("principal.tmpr"));
    }

    [Fact]
    public void Functions_IncludeOwnAndImportedWithTheirSignature()
    {
        FileOutline outline = OutlineOfProgram();

        Assert.Equal(["doble", "saludar"], outline.Functions.Select(function => function.Name).Order());
        Assert.Equal("sample : doble(sample x)", outline.Functions.Single(function => function.Name == "doble").Signature);
        Assert.Equal("utilidades.tm", outline.Functions.Single(function => function.Name == "doble").FileName);
    }

    [Fact]
    public void Functions_ExcludeThoseOfFilesThatAreNotImported()
    {
        FileOutline outline = OutlineOfProgram(("otro.tm", "void : compose aislada() >\n<\n"));

        Assert.DoesNotContain(outline.Functions, function => function.Name == "aislada");
    }

    [Fact]
    public void LocalsVisibleAt_InsideAFunction_IncludeParametersAndEarlierDeclarations()
    {
        FileOutline outline = OutlineOfProgram();

        Assert.Equal(["nombre"], outline.LocalsVisibleAt(2).Select(local => local.Name));
        Assert.Equal(["nombre", "VECES"], outline.LocalsVisibleAt(4).Select(local => local.Name));
        Assert.Equal(LocalKind.Constant, outline.LocalsVisibleAt(4).Single(local => local.Name == "VECES").Kind);
    }

    [Fact]
    public void LocalsVisibleAt_InsideSession_IncludeNestedAndLoopDeclarations()
    {
        FileOutline outline = OutlineOfProgram();

        Assert.Equal(["edad", "i", "texto"], outline.LocalsVisibleAt(11).Select(local => local.Name));
        Assert.Equal(["edad", "i", "texto", "listo"], outline.LocalsVisibleAt(13).Select(local => local.Name));
        Assert.Equal("sample", outline.LocalsVisibleAt(8).Single(local => local.Name == "i").TypeName);
    }

    [Fact]
    public void LocalsVisibleAt_DoNotLeakBetweenFunctionAndSession()
    {
        FileOutline outline = OutlineOfProgram();

        Assert.DoesNotContain(outline.LocalsVisibleAt(7), local => local.Name is "nombre" or "VECES");
        Assert.DoesNotContain(outline.LocalsVisibleAt(4), local => local.Name == "edad");
    }

    [Fact]
    public void OutlineOf_FileWithSyntaxError_IsNull()
    {
        using var project = ProjectWorkspace.With(("principal.tmpr", "session >\n    sample edad = ;\n<\n"));

        Assert.Null(project.CompileDirectory().OutlineOf(project.PathOf("principal.tmpr")));
    }

    [Fact]
    public void OutlineOf_FileOutsideTheProject_IsNull()
    {
        using var project = ProjectWorkspace.With(("principal.tmpr", "session >\n<\n"));

        Assert.Null(project.CompileDirectory().OutlineOf(project.PathOf("no-existe.tm")));
    }
}
