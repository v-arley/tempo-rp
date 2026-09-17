using TmCompiler.Domain.Diagnostics;
using TmCompiler.Domain.Semantic;
using TmCompiler.Domain.Syntactic;

namespace TmCompiler.Application.Analysis;

/// <summary>
/// Primera pasada del análisis: recolecta todas las firmas de función antes de
/// analizar ningún cuerpo. Es lo que permite invocar una función declarada más
/// abajo en el archivo, sin forward declarations (§5.3).
/// </summary>

public sealed class FunctionSignatureCollector
{
    private readonly SymbolTable _symbols;
    private readonly DiagnosticBag _diagnostics;

    public FunctionSignatureCollector(SymbolTable symbols, DiagnosticBag diagnostics)
    {
        _symbols = symbols;
        _diagnostics = diagnostics;
    }

    public void Collect(ProgramNode program)
    {
        foreach (FunctionDeclarationNode function in program.Functions)
        {
            Collect(function);
        }
    }

    /// <summary>
    /// Siembra firmas ya conocidas (por ejemplo, de archivos importados) para
    /// que choquen con las locales igual que lo harían dos funciones del mismo
    /// archivo.
    /// </summary>
    public void Seed(IReadOnlyCollection<FunctionSymbol> functions)
    {
        if (functions is null)
        {
            return;
        }

        foreach (FunctionSymbol function in functions)
        {
            _symbols.TryDeclareFunction(function, out _);
        }
    }

    private void Collect(FunctionDeclarationNode function)
    {
        var parameters = new List<VariableSymbol>(function.Parameters.Count);

        foreach (ParameterNode parameter in function.Parameters)
        {
            parameters.Add(
                new VariableSymbol(
                    parameter.Identifier,
                    TypeNameParser.ParseOrUnknown(parameter.TypeName),
                    SymbolKind.Parameter,
                    parameter.Line));
        }

        var symbol = new FunctionSymbol(
            function.Identifier,
            TypeNameParser.ParseOrUnknown(function.ReturnType),
            parameters,
            function.Line);

        if (!_symbols.TryDeclareFunction(symbol, out FunctionSymbol existing))
        {
            _diagnostics.ReportError(
                DiagnosticCodes.FunctionOverload,
                function.Line,
                $"La función '{function.Identifier}' ya se declaró en la línea {existing.Line}.",
                "TM no admite sobrecarga: cada función debe tener un nombre único.");
        }
    }
}
