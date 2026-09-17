using TmCompiler.Domain.Lexical;

namespace TmCompiler.Domain.Semantic;

/// <summary>
/// Clave de una fila de la tabla de operadores (§4.4.2). Se indexa por
/// <see cref="TypeKind"/> y no por <see cref="TmType"/> porque ningún operador
/// de TM admite tipos compuestos: los <c>sequence</c>/<c>score</c> se rechazan
/// antes de consultar la tabla.
/// </summary>

public readonly record struct OperatorSignature(TokenType Operator, TypeKind Left, TypeKind Right);
