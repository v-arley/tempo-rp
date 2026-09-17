namespace TmCompiler.Domain.Semantic;

/// <summary>
/// Un nivel de ámbito léxico. La resolución sube por la cadena de padres
/// hasta el ámbito más externo (§5.1).
/// </summary>

public sealed class Scope
{
    private readonly Dictionary<string, VariableSymbol> _symbols = new(StringComparer.Ordinal);

    public Scope(Scope parent)
    {
        Parent = parent;
    }

    public Scope Parent { get; }

    public IReadOnlyCollection<VariableSymbol> Symbols => _symbols.Values;

    /// <summary>Declara el símbolo salvo que ya exista uno homónimo en este nivel.</summary>
    public bool TryDeclare(VariableSymbol symbol)
    {
        return _symbols.TryAdd(symbol.Name, symbol);
    }

    /// <summary>Busca solo en este nivel, sin subir a los ámbitos externos.</summary>
    public VariableSymbol ResolveLocal(string name)
    {
        _symbols.TryGetValue(name, out VariableSymbol symbol);
        return symbol;
    }

    /// <summary>Busca en este nivel y, si no lo encuentra, en los ámbitos externos.</summary>
    public VariableSymbol Resolve(string name)
    {
        for (Scope scope = this; scope is not null; scope = scope.Parent)
        {
            VariableSymbol symbol = scope.ResolveLocal(name);

            if (symbol is not null)
            {
                return symbol;
            }
        }

        return null;
    }
}
