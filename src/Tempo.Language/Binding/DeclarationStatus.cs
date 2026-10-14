namespace Tempo.Language.Binding;

/// <summary>
/// Resultado de intentar declarar un símbolo en el ámbito actual.
/// </summary>

public enum DeclarationStatus
{
    /// <summary>Declarado sin conflictos.</summary>
    Declared,

    /// <summary>
    /// Declarado, pero oculta un símbolo homónimo de un ámbito externo.
    /// Es válido y obliga a advertir (§5.1.5).
    /// </summary>
    Shadowing,

    /// <summary>Ya existía en el mismo ámbito: no se declara.</summary>
    Duplicated
}
