namespace Tempo.Language.Syntax;

/// <summary>
/// Representa una declaración de función TM.
///
/// Ejemplo:
/// sample : compose sumar(sample a, sample b) >
///     ret a + b;
/// <
/// </summary>
public sealed record FunctionDeclarationNode(
    string ReturnType,
    string Identifier,
    IReadOnlyList<ParameterNode> Parameters,
    BlockNode Body,
    int Line,
    int Column
) : AstNode;