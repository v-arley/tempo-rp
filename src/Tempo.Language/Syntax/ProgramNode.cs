namespace Tempo.Language.Syntax;

/// <summary>
/// Representa un programa TM completo.
/// Contiene importaciones, la declaración tempo, funciones y la sentencia session.
/// </summary>
public sealed record ProgramNode(
    IReadOnlyList<ImportNode> Imports,
    IReadOnlyList<FunctionDeclarationNode> Functions,
    SessionNode Session
) : AstNode
{
    public IReadOnlyList<SessionNode> AdditionalSessions { get; init; }
        = Array.Empty<SessionNode>();

    /// <summary>Declaración <c>tempo</c> del archivo, o <c>null</c> si no la tiene.</summary>
    public TempoDeclarationNode Tempo { get; init; }

    /// <summary>Declaraciones <c>tempo</c> después de la primera; cada una produce E041.</summary>
    public IReadOnlyList<TempoDeclarationNode> AdditionalTempos { get; init; }
        = Array.Empty<TempoDeclarationNode>();

    /// <summary>Todas las declaraciones <c>tempo</c> del archivo, en orden de aparición.</summary>
    public IEnumerable<TempoDeclarationNode> AllTempos =>
        Tempo is null ? AdditionalTempos : AdditionalTempos.Prepend(Tempo);
}