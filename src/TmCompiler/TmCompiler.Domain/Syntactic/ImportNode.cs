namespace TmCompiler.Domain.Syntactic;

/// <summary>
/// Representa una importación de otro archivo TM.
///
/// Ejemplo:
/// import "utilidades.tm";
/// </summary>
public sealed record ImportNode(
    string FilePath,
    int Line,
    int Column
) : AstNode;