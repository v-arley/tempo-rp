using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace TmIde.Converters;

/// <summary>
/// Convierte una clave de recurso (string) en la <see cref="Geometry"/>
/// correspondiente de Icons.xaml. Se usa para pintar el icono de cada nodo del
/// explorador sin acoplar el ViewModel a WPF.
/// </summary>

public sealed class ResourceKeyToGeometryConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string key && Application.Current?.TryFindResource(key) is Geometry geometry)
        {
            return geometry;
        }

        return Binding.DoNothing;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
