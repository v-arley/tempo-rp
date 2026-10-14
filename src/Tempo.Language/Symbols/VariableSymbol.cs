namespace Tempo.Language.Symbols;

/// <summary>
/// Símbolo de variable, constante o parámetro. Es una clase y no un record
/// porque <see cref="IsUsed"/> se actualiza durante el recorrido del AST para
/// poder emitir la advertencia de variable no utilizada.
/// </summary>

public sealed class VariableSymbol
{
    public VariableSymbol(string name, TmType type, SymbolKind kind, int line)
    {
        Name = name;
        Type = type ?? TmType.Unknown;
        Kind = kind;
        Line = line;
    }

    public string Name { get; }

    public TmType Type { get; }

    public SymbolKind Kind { get; }

    public int Line { get; }

    /// <summary>Marcado al leer el símbolo; escribir en él no cuenta como uso.</summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Las constantes son inmutables (§4.3). Los parámetros se tratan como
    /// mutables: la §5.3 define el paso por valor, de modo que modificarlos
    /// dentro de la función es legal y no afecta al argumento original.
    /// </summary>
    public bool IsReadOnly => Kind == SymbolKind.Constant;
}
