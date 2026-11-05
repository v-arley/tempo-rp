namespace Tempo.Language.Syntax;

/// <summary>
/// Representa una sentencia que lanza una ráfaga de partículas en el panel
/// del IDE. La intensidad es una expresión numérica que se limita a [0, 100].
///
/// Ejemplo:
/// pulse [raiz + 4];
/// </summary>
public sealed record PulseStatementNode(
    ExpressionNode Intensity,
    int Line,
    int Column
) : StatementNode;
