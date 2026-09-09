using TmCompiler.Domain.Lexical;

namespace TmCompiler.Domain.Syntactic;

/// <summary>
/// Representa una actualización unitaria como sentencia.
///
/// Ejemplos:
/// contador++;
/// contador--;
/// </summary>
public sealed record UpdateStatementNode(
    string Identifier,
    TokenType Operator,
    int Line,
    int Column
) : StatementNode;