using TmCompiler.Domain.Diagnostics;
using TmCompiler.Domain.Semantic;

namespace TmCompiler.Application.Analysis;

/// <summary>
/// Salida del análisis semántico de un archivo.
/// <see cref="DeclaredFunctions"/> y <see cref="UnresolvedFunctionCalls"/> son
/// el punto de enganche para el análisis de proyecto multiarchivo: permiten
/// fusionar ámbitos globales entre archivos sin rehacer esta fase.
/// </summary>

public sealed record SemanticAnalysisResult(
    IReadOnlyList<Diagnostic> Diagnostics,
    IReadOnlyCollection<FunctionSymbol> DeclaredFunctions,
    IReadOnlyCollection<string> UnresolvedFunctionCalls)
{
    public bool HasErrors
    {
        get
        {
            foreach (Diagnostic diagnostic in Diagnostics)
            {
                if (diagnostic.IsError)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
