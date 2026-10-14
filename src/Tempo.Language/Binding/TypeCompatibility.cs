using Tempo.Language.Symbols;

namespace Tempo.Language.Binding;

/// <summary>
/// Reglas de asignabilidad entre tipos (§4.4.1). TM es de tipado fuerte y no
/// tiene coerción implícita: <c>sample</c> y <c>wave</c> son incompatibles en
/// ambos sentidos. La única conversión en asignación es <c>silence</c> hacia
/// <c>phrase</c>.
/// </summary>

public static class TypeCompatibility
{
    public static bool IsAssignable(TmType target, TmType source)
    {
        if (target is null || source is null)
        {
            return true;
        }

        // Unknown es el tipo error: se propaga sin generar diagnósticos nuevos.
        if (target.IsUnknown || source.IsUnknown)
        {
            return true;
        }

        if (source.Kind == TypeKind.Silence)
        {
            return target.Kind == TypeKind.Phrase;
        }

        return target == source;
    }

    /// <summary>
    /// Indica si se está asignando <c>silence</c> a un tipo que no lo admite,
    /// caso que la especificación distingue con su propio código (E010).
    /// </summary>
    public static bool IsSilenceMisuse(TmType target, TmType source)
    {
        if (target is null || source is null || target.IsUnknown)
        {
            return false;
        }

        return source.Kind == TypeKind.Silence && target.Kind != TypeKind.Phrase;
    }
}
