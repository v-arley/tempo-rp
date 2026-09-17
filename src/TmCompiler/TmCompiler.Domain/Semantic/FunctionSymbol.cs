namespace TmCompiler.Domain.Semantic;

/// <summary>
/// Firma de una función recolectada en la primera pasada del análisis
/// semántico. TM no admite sobrecarga: el nombre es único en todo el proyecto.
/// </summary>

public sealed class FunctionSymbol
{
    public FunctionSymbol(
        string name,
        TmType returnType,
        IReadOnlyList<VariableSymbol> parameters,
        int line)
    {
        Name = name;
        ReturnType = returnType ?? TmType.Unknown;
        Parameters = parameters ?? Array.Empty<VariableSymbol>();
        Line = line;
    }

    public string Name { get; }

    public TmType ReturnType { get; }

    public IReadOnlyList<VariableSymbol> Parameters { get; }

    public int Line { get; }

    /// <summary>Marcado ante cualquier llamada, incluida la recursiva.</summary>
    public bool IsInvoked { get; set; }
}
