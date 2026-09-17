namespace TmCompiler.Domain.Semantic;

/// <summary>
/// Pila de ámbitos léxicos más el espacio de nombres de funciones. Las
/// funciones no se apilan: TM tiene un único espacio global de funciones,
/// visible desde cualquier punto gracias al análisis en dos pasadas (§5.3).
/// </summary>

public sealed class SymbolTable
{
    private readonly Dictionary<string, FunctionSymbol> _functions = new(StringComparer.Ordinal);
    private Scope _currentScope = new(null);

    public IReadOnlyCollection<FunctionSymbol> Functions => _functions.Values;

    public void EnterScope()
    {
        _currentScope = new Scope(_currentScope);
    }

    /// <summary>
    /// Cierra el ámbito actual y devuelve sus símbolos, para que el analizador
    /// decida qué advertir sobre ellos (la capa de dominio no conoce los
    /// diagnósticos).
    /// </summary>
    public IReadOnlyCollection<VariableSymbol> ExitScope()
    {
        IReadOnlyCollection<VariableSymbol> symbols = _currentScope.Symbols;

        if (_currentScope.Parent is not null)
        {
            _currentScope = _currentScope.Parent;
        }

        return symbols;
    }

    public DeclarationResult DeclareVariable(VariableSymbol symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        VariableSymbol duplicated = _currentScope.ResolveLocal(symbol.Name);

        if (duplicated is not null)
        {
            return new DeclarationResult(DeclarationStatus.Duplicated, duplicated);
        }

        VariableSymbol shadowed = _currentScope.Parent?.Resolve(symbol.Name);

        _currentScope.TryDeclare(symbol);

        return shadowed is not null
            ? new DeclarationResult(DeclarationStatus.Shadowing, shadowed)
            : new DeclarationResult(DeclarationStatus.Declared, null);
    }

    public VariableSymbol ResolveVariable(string name)
    {
        return _currentScope.Resolve(name);
    }

    public bool TryDeclareFunction(FunctionSymbol symbol, out FunctionSymbol existing)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        if (_functions.TryGetValue(symbol.Name, out existing))
        {
            return false;
        }

        _functions.Add(symbol.Name, symbol);
        existing = null;
        return true;
    }

    public FunctionSymbol ResolveFunction(string name)
    {
        if (name is null)
        {
            return null;
        }

        _functions.TryGetValue(name, out FunctionSymbol symbol);
        return symbol;
    }
}
