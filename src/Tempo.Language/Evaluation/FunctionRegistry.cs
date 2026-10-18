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

    private readonly Dictionary<string, string> _sourceFiles =
        new(StringComparer.Ordinal);    

    public FunctionRegistry(
        IEnumerable<FunctionDeclarationNode> functions,
        IReadOnlyDictionary<string, string>? sourceFiles = null)
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

            if (sourceFiles is not null &&
                sourceFiles.TryGetValue(
                    function.Identifier,
                    out string? fileName) &&
                !string.IsNullOrWhiteSpace(fileName))
            {
                _sourceFiles.Add(
                    function.Identifier,
                    fileName);
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

    public string? GetFileName(string identifier)
    {
        return _sourceFiles.TryGetValue(
            identifier,
            out string? fileName)
            ? fileName
            : null;
    }
}