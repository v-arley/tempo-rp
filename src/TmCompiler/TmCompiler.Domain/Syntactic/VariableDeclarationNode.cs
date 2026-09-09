namespace TmCompiler.Domain.Syntactic;

/// <summary>
/// Representa una declaración de variable TM.
///
/// Ejemplo:
/// sample edad = 22;
/// </summary>
public sealed record VariableDeclarationNode(
    string TypeName,
    string Identifier,
    ExpressionNode Initializer,
    int Line,
    int Column
) : StatementNode;