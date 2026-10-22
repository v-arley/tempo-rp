namespace TmIde.Services;

/// <summary>
/// Cambia el tema visual en caliente intercambiando el ResourceDictionary de
/// tema en <c>Application.Current.Resources.MergedDictionaries</c>. Las vistas
/// consumen las claves con <c>{DynamicResource}</c>, por lo que reaccionan al
/// swap sin reiniciar. El tema elegido se recuerda entre sesiones.
/// </summary>

public interface IThemeService
{
    AppTheme Current { get; }

    /// <summary>Aplica el tema guardado por el usuario o, si no hay, el de la configuración.</summary>
    void ApplySaved();

    /// <summary>Aplica <paramref name="theme"/> y lo guarda como preferencia.</summary>
    void Apply(AppTheme theme);

    /// <summary>Alterna entre <see cref="AppTheme.Light"/> y <see cref="AppTheme.Dark"/>.</summary>
    void Toggle();
}
