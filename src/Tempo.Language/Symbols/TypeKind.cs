namespace Tempo.Language.Symbols;

/// <summary>
/// Categorías del sistema de tipos de TM (§4 de la especificación v1.1).
/// </summary>

public enum TypeKind
{
    /// <summary>Entero con signo de 32 bits.</summary>
    Sample,

    /// <summary>Punto flotante de precisión simple (IEEE 754, 32 bits).</summary>
    Wave,

    /// <summary>Carácter Unicode.</summary>
    Tone,

    /// <summary>Cadena de texto mutable.</summary>
    Phrase,

    /// <summary>Booleano.</summary>
    Signal,

    /// <summary>Valor nulo; solo asignable a <see cref="Phrase"/>.</summary>
    Silence,

    /// <summary>Retorno vacío; válido únicamente como tipo de retorno de función.</summary>
    Void,

    /// <summary>Arreglo unidimensional homogéneo de tamaño fijo.</summary>
    Sequence,

    /// <summary>Matriz bidimensional homogénea de tamaño fijo.</summary>
    Score,

    /// <summary>
    /// Tipo error. Se propaga en silencio para evitar diagnósticos derivados
    /// de un error ya reportado.
    /// </summary>
    Unknown
}
