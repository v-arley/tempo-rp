namespace TmCompiler.Domain.Semantic;

/// <summary>
/// Tipo del lenguaje TM. Al ser un record, la igualdad es estructural y
/// recursiva: <c>SequenceOf(Sample) == SequenceOf(Sample)</c> sin escribir
/// comparadores. Los tipos no compuestos son instancias únicas.
/// </summary>

public sealed record TmType
{
    private TmType(TypeKind kind, TmType elementType)
    {
        Kind = kind;
        ElementType = elementType;
    }

    public static TmType Sample { get; } = new(TypeKind.Sample, null);
    public static TmType Wave { get; } = new(TypeKind.Wave, null);
    public static TmType Tone { get; } = new(TypeKind.Tone, null);
    public static TmType Phrase { get; } = new(TypeKind.Phrase, null);
    public static TmType Signal { get; } = new(TypeKind.Signal, null);
    public static TmType Silence { get; } = new(TypeKind.Silence, null);
    public static TmType Void { get; } = new(TypeKind.Void, null);
    public static TmType Unknown { get; } = new(TypeKind.Unknown, null);

    public TypeKind Kind { get; }

    /// <summary>Tipo de los elementos; solo para <c>sequence</c> y <c>score</c>.</summary>
    public TmType ElementType { get; }

    public bool IsComposite => Kind is TypeKind.Sequence or TypeKind.Score;

    public bool IsUnknown => Kind == TypeKind.Unknown;

    /// <summary>
    /// Tipos primitivos según §4.1. <c>silence</c>, <c>void</c> y los compuestos
    /// quedan fuera: no participan en la tabla de operadores ni en el selector
    /// de <c>mix</c>.
    /// </summary>
    public bool IsPrimitive => Kind is TypeKind.Sample
        or TypeKind.Wave
        or TypeKind.Tone
        or TypeKind.Phrase
        or TypeKind.Signal;

    public static TmType SequenceOf(TmType elementType)
    {
        return new TmType(TypeKind.Sequence, elementType ?? Unknown);
    }

    public static TmType ScoreOf(TmType elementType)
    {
        return new TmType(TypeKind.Score, elementType ?? Unknown);
    }

    /// <summary>Devuelve la ortografía del tipo tal y como se escribe en TM.</summary>
    public override string ToString()
    {
        switch (Kind)
        {
            case TypeKind.Sequence:
                return $"sequence<{ElementType}>";

            case TypeKind.Score:
                return $"score<{ElementType}>";

            case TypeKind.Unknown:
                return "desconocido";

            default:
                return Kind.ToString().ToLowerInvariant();
        }
    }
}
