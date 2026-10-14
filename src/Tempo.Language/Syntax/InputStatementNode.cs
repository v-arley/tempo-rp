namespace Tempo.Language.Syntax;

/// <summary>
/// Representa una sentencia de entrada TM.
///
/// Ejemplo:
/// input << edad;
/// </summary>
public sealed record InputStatementNode(
    string Identifier,
    int Line,
    int Column
) : StatementNode;