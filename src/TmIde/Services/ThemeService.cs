using System;
using System.Linq;
using System.Windows;

using Microsoft.Extensions.Options;

using TmIde.Services.Settings;

namespace TmIde.Services;

/// <inheritdoc cref="IThemeService" />
public sealed class ThemeService : IThemeService
{
    private const string ThemeDictionaryMarker = "Resources/Theme.";

    private readonly IUserSettingsStore _settings;
    private readonly IOptions<IdeOptions> _options;

    public ThemeService(IUserSettingsStore settings, IOptions<IdeOptions> options)
    {
        _settings = settings;
        _options = options;
    }

    public AppTheme Current { get; private set; } = AppTheme.Light;

    // Al arrancar no se escribe nada: solo se aplica lo que ya estaba guardado.
    public void ApplySaved() => ApplyResources(_settings.Load().Theme ?? _options.Value.DefaultTheme);

    public void Apply(AppTheme theme)
    {
        ApplyResources(theme);
        _settings.Save(_settings.Load() with { Theme = theme });
    }

    public void Toggle() => Apply(Current == AppTheme.Light ? AppTheme.Dark : AppTheme.Light);

    private void ApplyResources(AppTheme theme)
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

        // Los controles reaccionan solos: sus pinceles están enlazados con
        // DynamicResource y WPF los actualiza al reemplazar el diccionario.
        Current = theme;
    }
}
