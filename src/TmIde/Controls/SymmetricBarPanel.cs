using System.Windows;
using System.Windows.Controls;

namespace TmIde.Controls;

/// <summary>
/// Distribución de una barra (header o footer) con tres hijos, en este orden:
/// inicio, centro y fin. El centro queda siempre centrado respecto al ancho total
/// del panel: mide <see cref="CenterRatio"/> del ancho, pero nunca invade el
/// lateral más ancho, de modo que se reduce por igual a ambos lados cuando falta
/// espacio. El inicio se ancla a la izquierda y el fin a la derecha, con su ancho
/// deseado, así que los laterales nunca se recortan.
/// </summary>
internal sealed class SymmetricBarPanel : Panel
{
    public static readonly DependencyProperty CenterRatioProperty = DependencyProperty.Register(
        nameof(CenterRatio),
        typeof(double),
        typeof(SymmetricBarPanel),
        new FrameworkPropertyMetadata(0.5, FrameworkPropertyMetadataOptions.AffectsArrange));

    /// <summary>Fracción del ancho total que ocupa el centro cuando hay espacio.</summary>
    public double CenterRatio
    {
        get => (double)GetValue(CenterRatioProperty);
        set => SetValue(CenterRatioProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var height = 0.0;
        foreach (UIElement child in InternalChildren)
        {
            child.Measure(availableSize);
            height = Math.Max(height, child.DesiredSize.Height);
        }

        var width = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (InternalChildren.Count != 3)
        {
            return finalSize;
        }

        var start = InternalChildren[0];
        var center = InternalChildren[1];
        var end = InternalChildren[2];

        var widestSide = Math.Max(start.DesiredSize.Width, end.DesiredSize.Width);
        var centerWidth = Math.Clamp(finalSize.Width - 2 * widestSide, 0, finalSize.Width * CenterRatio);

        start.Arrange(new Rect(0, 0, start.DesiredSize.Width, finalSize.Height));
        center.Arrange(new Rect((finalSize.Width - centerWidth) / 2, 0, centerWidth, finalSize.Height));
        end.Arrange(new Rect(finalSize.Width - end.DesiredSize.Width, 0, end.DesiredSize.Width, finalSize.Height));

        return finalSize;
    }
}