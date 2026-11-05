using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

namespace TmIde.Controls.Particles;

/// <summary>
/// Los dos tonos con que se sombrea la nube: <see cref="Lit"/> en la cara que
/// mira a la luz y <see cref="Shadow"/> en la opuesta (<see cref="For"/>).
/// Correcto y Error comparten colores: lo que delata un error es la
/// deformación de la nube, no el color. Las advertencias sí cambian el color.
/// </summary>

public sealed record DotPalette(DotColor Lit, DotColor Shadow)
{
    // Los tonos de sombra son de luminosidad media: contrastan tanto con la
    // superficie clara (#A4B1C7) como con la oscura (#262D36).

    // Rojo a magenta.
    private static readonly DotPalette Crimson = new(new DotColor(255, 72, 72), new DotColor(168, 36, 148));

    // Ámbar a naranja quemado.
    private static readonly DotPalette Amber = new(new DotColor(255, 196, 64), new DotColor(200, 88, 24));

    private static readonly FrozenDictionary<DiagnosticState, DotPalette> ByState =
        new Dictionary<DiagnosticState, DotPalette>
        {
            [DiagnosticState.Correct] = Crimson,
            [DiagnosticState.Warning] = Amber,
            [DiagnosticState.Error] = Crimson,
        }.ToFrozenDictionary();

    public static DotPalette For(DiagnosticState state) => ByState[state];

    /// <summary>Tono con <paramref name="shadow"/> de sombra: 0 a plena luz, 1 en sombra total.</summary>
    public DotColor At(float shadow) => Lit.Toward(Shadow, shadow);

    /// <summary>Promedio de paletas pesado; los pesos deben sumar 1.</summary>
    public static DotPalette Blend(IReadOnlyCollection<(DotPalette Palette, float Weight)> parts) => new(
        DotColor.Blend(parts.Select(part => (part.Palette.Lit, part.Weight))),
        DotColor.Blend(parts.Select(part => (part.Palette.Shadow, part.Weight))));
}
