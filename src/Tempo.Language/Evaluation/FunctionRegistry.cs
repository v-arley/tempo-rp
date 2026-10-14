using System.Diagnostics.CodeAnalysis;
using Tempo.Language.Syntax;

namespace Tempo.Language.Evaluation;

/// <summary>
/// Mantiene las funciones disponibles durante la ejecución.
/// </summary>
public sealed class FunctionRegistry
{
    private readonly Dictionary<string, FunctionDeclarationNode> _functions =
        new(StringComparer.Ordinal);

    public FunctionRegistry(
        IEnumerable<FunctionDeclarationNode> functions)
    {
        ArgumentNullException.ThrowIfNull(functions);

        foreach (FunctionDeclarationNode function in functions)
        {
            if (!_functions.TryAdd(
                    function.Identifier,
                    function))
            {
                throw new InvalidOperationException(
                    $"La función '{function.Identifier}' " +
                    "ya está registrada.");
            }
        }
    }

    public bool TryResolve(
        string identifier,
        [NotNullWhen(true)]
        out FunctionDeclarationNode? function)
    {
        return _functions.TryGetValue(
            identifier,
            out function);
    }
}