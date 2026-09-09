namespace TmCompiler.Domain.Syntactic;

/// <summary>
/// Representa un programa TM completo.
/// Contiene importaciones, funciones y la sentencia session.
/// </summary>
public sealed record ProgramNode(
    IReadOnlyList<ImportNode> Imports,
    IReadOnlyList<FunctionDeclarationNode> Functions,
    SessionNode Session
) : AstNode;

/// <summary>
/// Representa la sentencia session del lenguaje TM.
/// </summary>
public sealed record SessionNode(
    BlockNode Body
) : AstNode;