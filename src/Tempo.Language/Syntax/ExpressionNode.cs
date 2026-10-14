namespace Tempo.Language.Syntax;

/// <summary>
/// Nodo base para todas las expresiones del lenguaje TM.
/// Ejemplos: 10, "Hola", x, a + b, etc.
/// </summary>
public abstract record ExpressionNode : AstNode;