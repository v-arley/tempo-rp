using System;
using System.Linq;
using System.Windows;

namespace TmIde.Services;

/// <inheritdoc cref="IThemeService" />
public sealed class ThemeService : IThemeService
{
    private const string ThemeDictionaryMarker = "Resources/Theme.";

    /// <summary>Se dispara tras reemplazar el diccionario de tema; lo usan los controles
    /// con pinceles que no son propiedades de dependencia (p. ej. el editor).</summary>
    public static event EventHandler? ThemeApplied;

    public AppTheme Current { get; private set; } = AppTheme.Light;

    public void Apply(AppTheme theme)
    {
        var app = Application.Current
            ?? throw new InvalidOperationException("No hay Application activa.");

        var merged = app.Resources.MergedDictionaries;

        var newDictionary = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/TmIde;component/Resources/Theme.{theme}.xaml", UriKind.Absolute),
        };

        var previous = merged.FirstOrDefault(
            d => d.Source is not null &&
                d.Source.OriginalString.Contains(ThemeDictionaryMarker, StringComparison.OrdinalIgnoreCase));

        // Insertar el nuevo antes de quitar el viejo evita un frame sin brushes.
        merged.Add(newDictionary);
        if (previous is not null)
        {
            merged.Remove(previous);
        }

        Current = theme;
        ThemeApplied?.Invoke(this, EventArgs.Empty);
    }

    public void Toggle() => Apply(Current == AppTheme.Light ? AppTheme.Dark : AppTheme.Light);
}
