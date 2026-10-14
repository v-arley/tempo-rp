using Tempo.Language.Binding;
using Tempo.Language.Syntax;
using Xunit;

namespace Tempo.Language.Tests.Binding;

/// <summary>
/// Análisis de retorno definitivo (§5.3). Es puramente estructural: no evalúa
/// condiciones, de modo que un bucle nunca garantiza retorno.
/// </summary>

public sealed class ReturnPathAnalyzerTests
{
    [Fact]
    public void AlwaysReturns_BodyEndingInReturn_IsTrue()
    {
        Assert.True(AlwaysReturns("    ret 1;"));
    }

    [Fact]
    public void AlwaysReturns_BodyWithoutReturn_IsFalse()
    {
        Assert.False(AlwaysReturns("    output >> 1;"));
    }

    [Fact]
    public void AlwaysReturns_IfWithoutElse_IsFalse()
    {
        Assert.False(
            AlwaysReturns(
                """
                    if [n > 0] >
                        ret 1;
                    <
                """));
    }

    [Fact]
    public void AlwaysReturns_IfWithElseAndBothBranchesReturning_IsTrue()
    {
        Assert.True(
            AlwaysReturns(
                """
                    if [n > 0] >
                        ret 1;
                    <
                    else >
                        ret 0;
                    <
                """));
    }

    [Fact]
    public void AlwaysReturns_IfWithElseIfThatDoesNotReturn_IsFalse()
    {
        Assert.False(
            AlwaysReturns(
                """
                    if [n > 0] >
                        ret 1;
                    <
                    else if [n < 0] >
                        output >> 0;
                    <
                    else >
                        ret 0;
                    <
                """));
    }

    [Fact]
    public void AlwaysReturns_IfWithoutReturnFollowedByReturn_IsTrue()
    {
        // Es el patrón de la función factorial de la especificación.
        Assert.True(
            AlwaysReturns(
                """
                    if [n <= 1] >
                        ret 1;
                    <
                    ret n * 2;
                """));
    }

    [Fact]
    public void AlwaysReturns_RepeatWithReturnInside_IsFalse()
    {
        // La condición podría ser falsa desde el principio.
        Assert.False(
            AlwaysReturns(
                """
                    repeat [n > 0] >
                        ret 1;
                    <
                """));
    }

    [Fact]
    public void AlwaysReturns_LoopWithReturnInside_IsFalse()
    {
        Assert.False(
            AlwaysReturns(
                """
                    loop [sample i = 0; i < n; i++] >
                        ret 1;
                    <
                """));
    }

    [Fact]
    public void AlwaysReturns_MixWithDefaultAndAllBranchesReturning_IsTrue()
    {
        Assert.True(
            AlwaysReturns(
                """
                    mix n >
                        case 1: >
                            ret 1;
                        <
                        default: >
                            ret 0;
                        <
                    <
                """));
    }

    [Fact]
    public void AlwaysReturns_MixWithoutDefault_IsFalse()
    {
        Assert.False(
            AlwaysReturns(
                """
                    mix n >
                        case 1: >
                            ret 1;
                        <
                    <
                """));
    }

    [Fact]
    public void AlwaysReturns_NullBlock_IsFalse()
    {
        Assert.False(ReturnPathAnalyzer.AlwaysReturns(null));
    }

    private static bool AlwaysReturns(string body)
    {
        BlockNode functionBody = SemanticTestHarness.FirstFunctionBody(
            $"sample : compose prueba(sample n) >\n{body}\n<\n\nsession >\n<");

        return ReturnPathAnalyzer.AlwaysReturns(functionBody);
    }
}
