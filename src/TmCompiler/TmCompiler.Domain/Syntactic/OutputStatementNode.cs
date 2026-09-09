namespace TmCompiler.Domain.Syntactic;

/// <summary>
/// Representa una sentencia de salida TM.
///
/// Ejemplo:
/// output >> "Hola";
/// output >> x + 10;
/// </summary>
public sealed record OutputStatementNode(
    ExpressionNode Expression,
    int Line,
    int Column
) : StatementNode;