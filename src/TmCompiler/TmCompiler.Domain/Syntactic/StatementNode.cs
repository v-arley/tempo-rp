namespace TmCompiler.Domain.Syntactic;

/// <summary>
/// Nodo base para todas las sentencias del lenguaje TM.
/// Ejemplos futuros: declaraciones, output, input, if, loop, etc.
/// </summary>
public abstract record StatementNode : AstNode;