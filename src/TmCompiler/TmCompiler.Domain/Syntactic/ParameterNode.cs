namespace TmCompiler.Domain.Syntactic;

/// <summary>
/// Representa un parámetro de una función TM.
///
/// Ejemplo:
/// sample edad
/// </summary>
public sealed record ParameterNode(
    string TypeName,
    string Identifier,
    int Line,
    int Column
) : AstNode;