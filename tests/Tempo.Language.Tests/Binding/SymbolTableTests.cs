using Tempo.Language.Binding;
using Tempo.Language.Symbols;
using Xunit;

namespace Tempo.Language.Tests.Binding;

public sealed class SymbolTableTests
{
    [Fact]
    public void DeclareVariable_NewName_ReturnsDeclared()
    {
        var table = new SymbolTable();

        DeclarationResult result = table.DeclareVariable(Variable("x"));

        Assert.Equal(DeclarationStatus.Declared, result.Status);
        Assert.Null(result.Existing);
    }

    [Fact]
    public void DeclareVariable_NameAlreadyInSameScope_ReturnsDuplicated()
    {
        var table = new SymbolTable();
        table.DeclareVariable(Variable("x", line: 3));

        DeclarationResult result = table.DeclareVariable(Variable("x", line: 7));

        Assert.Equal(DeclarationStatus.Duplicated, result.Status);
        Assert.Equal(3, result.Existing.Line);
    }

    [Fact]
    public void DeclareVariable_NameFromOuterScope_ReturnsShadowingWithOriginal()
    {
        var table = new SymbolTable();
        table.DeclareVariable(Variable("x", line: 2));
        table.EnterScope();

        DeclarationResult result = table.DeclareVariable(Variable("x", line: 9));

        Assert.Equal(DeclarationStatus.Shadowing, result.Status);
        Assert.Equal(2, result.Existing.Line);
    }

    [Fact]
    public void ResolveVariable_DeclaredInOuterScope_IsVisibleFromInnerScope()
    {
        var table = new SymbolTable();
        table.DeclareVariable(Variable("x"));
        table.EnterScope();
        table.EnterScope();

        Assert.NotNull(table.ResolveVariable("x"));
    }

    [Fact]
    public void ResolveVariable_DeclaredInInnerScope_IsNotVisibleAfterExit()
    {
        var table = new SymbolTable();
        table.EnterScope();
        table.DeclareVariable(Variable("interna"));
        table.ExitScope();

        Assert.Null(table.ResolveVariable("interna"));
    }

    [Fact]
    public void ResolveVariable_NameDiffersInCase_IsNotFound()
    {
        var table = new SymbolTable();
        table.DeclareVariable(Variable("total"));

        // TM distingue mayúsculas y minúsculas.
        Assert.Null(table.ResolveVariable("Total"));
    }

    [Fact]
    public void ExitScope_ReturnsSymbolsOfTheClosedScope()
    {
        var table = new SymbolTable();
        table.EnterScope();
        table.DeclareVariable(Variable("a"));
        table.DeclareVariable(Variable("b"));

        IReadOnlyCollection<VariableSymbol> symbols = table.ExitScope();

        Assert.Equal(2, symbols.Count);
    }

    [Fact]
    public void TryDeclareFunction_DuplicatedName_ReturnsFalseWithExisting()
    {
        var table = new SymbolTable();
        table.TryDeclareFunction(Function("sumar", line: 1), out _);

        bool declared = table.TryDeclareFunction(
            Function("sumar", line: 10),
            out FunctionSymbol existing);

        Assert.False(declared);
        Assert.Equal(1, existing.Line);
    }

    [Fact]
    public void ResolveFunction_FromAnyScopeDepth_IsVisible()
    {
        var table = new SymbolTable();
        table.TryDeclareFunction(Function("sumar"), out _);
        table.EnterScope();
        table.EnterScope();

        // Las funciones viven en un único espacio global, no en la pila de ámbitos.
        Assert.NotNull(table.ResolveFunction("sumar"));
    }

    [Fact]
    public void IsReadOnly_ForConstantAndParameter_FollowsSpecification()
    {
        Assert.True(new VariableSymbol("X", TmType.Sample, SymbolKind.Constant, 1).IsReadOnly);
        Assert.False(new VariableSymbol("p", TmType.Sample, SymbolKind.Parameter, 1).IsReadOnly);
        Assert.False(new VariableSymbol("v", TmType.Sample, SymbolKind.Variable, 1).IsReadOnly);
    }

    private static VariableSymbol Variable(string name, int line = 1)
    {
        return new VariableSymbol(name, TmType.Sample, SymbolKind.Variable, line);
    }

    private static FunctionSymbol Function(string name, int line = 1)
    {
        return new FunctionSymbol(name, TmType.Sample, Array.Empty<VariableSymbol>(), line);
    }
}
