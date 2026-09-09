namespace TmCompiler.Domain.Syntactic;

/// <summary>
/// Representa una declaración de constante TM.
///
/// Ejemplo:
/// const sample MAX = 100;
/// </summary>
public sealed record ConstantDeclarationNode(
    string TypeName,
    string Identifier,
    ExpressionNode Initializer,
    int Line,
    int Column
) : StatementNode;