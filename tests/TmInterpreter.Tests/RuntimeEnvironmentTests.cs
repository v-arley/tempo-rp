using TmInterpreter.Domain;
using Xunit;

namespace TmInterpreter.Tests;

public sealed class RuntimeEnvironmentTests
{
    [Fact]
    public void DeclareStoresVariableInCurrentEnvironment()
    {
        var environment = new RuntimeEnvironment();

        environment.Declare(
            "numero",
            RuntimeValue.Sample(10));

        RuntimeVariable variable =
            environment.Resolve("numero");

        Assert.Equal("numero", variable.Name);
        Assert.Equal(RuntimeValueKind.Sample, variable.Value.Kind);
        Assert.Equal(10, variable.Value.Value);
        Assert.False(variable.IsConstant);
    }

    [Fact]
    public void DeclareConstantStoresConstantFlag()
    {
        var environment = new RuntimeEnvironment();

        environment.Declare(
            "MAX",
            RuntimeValue.Sample(100),
            true);

        RuntimeVariable variable =
            environment.Resolve("MAX");

        Assert.True(variable.IsConstant);
    }

    [Fact]
    public void ResolveFindsVariableFromParentEnvironment()
    {
        var parent = new RuntimeEnvironment();

        parent.Declare(
            "numero",
            RuntimeValue.Sample(20));

        var child = new RuntimeEnvironment(parent);

        RuntimeVariable variable =
            child.Resolve("numero");

        Assert.Equal(20, variable.Value.Value);
    }

    [Fact]
    public void DeclareAllowsShadowingInChildEnvironment()
    {
        var parent = new RuntimeEnvironment();

        parent.Declare(
            "numero",
            RuntimeValue.Sample(10));

        var child = new RuntimeEnvironment(parent);

        child.Declare(
            "numero",
            RuntimeValue.Sample(99));

        Assert.Equal(
            99,
            child.Resolve("numero").Value.Value);

        Assert.Equal(
            10,
            parent.Resolve("numero").Value.Value);
    }

    [Fact]
    public void DeclareDuplicateInSameEnvironmentThrows()
    {
        var environment = new RuntimeEnvironment();

        environment.Declare(
            "numero",
            RuntimeValue.Sample(10));

        Assert.Throws<InvalidOperationException>(() =>
            environment.Declare(
                "numero",
                RuntimeValue.Sample(20)));
    }

    [Fact]
    public void ResolveUnknownVariableThrows()
    {
        var environment = new RuntimeEnvironment();

        Assert.Throws<InvalidOperationException>(() =>
            environment.Resolve("inexistente"));
    }
}