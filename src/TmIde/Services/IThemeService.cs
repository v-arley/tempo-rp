namespace TmIde.Services;

/// <summary>
/// Cambia el tema visual en caliente intercambiando el ResourceDictionary de
/// tema en <c>Application.Current.Resources.MergedDictionaries</c>. Las vistas
/// consumen las claves con <c>{DynamicResource}</c>, por lo que reaccionan al
/// swap sin reiniciar.
/// </summary>

public interface IThemeService
{
    AppTheme Current { get; }

    void Apply(AppTheme theme);

    /// <summary>Alterna entre <see cref="AppTheme.Light"/> y <see cref="AppTheme.Dark"/>.</summary>
    void Toggle();
}
