namespace Tempo.Language.Syntax;

/// <summary>
/// Representa un programa TM completo.
/// Contiene importaciones, funciones y la sentencia session.
/// </summary>
public sealed record ProgramNode(
    IReadOnlyList<ImportNode> Imports,
    IReadOnlyList<FunctionDeclarationNode> Functions,
    SessionNode Session
) : AstNode
{
    public IReadOnlyList<SessionNode> AdditionalSessions { get; init; }
        = Array.Empty<SessionNode>();
}