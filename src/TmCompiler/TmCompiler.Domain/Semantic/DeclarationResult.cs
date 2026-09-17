namespace TmCompiler.Domain.Semantic;

/// <summary>
/// Desenlace de una declaración. <see cref="Existing"/> trae el símbolo en
/// conflicto (el duplicado o el que se está sombreando) para poder citar su
/// línea en el diagnóstico.
/// </summary>

public readonly record struct DeclarationResult(DeclarationStatus Status, VariableSymbol Existing);
