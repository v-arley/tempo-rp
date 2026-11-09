namespace TmIde.Services;

/// <summary>Nombre visible de cada <see cref="AppTheme"/> en la interfaz.</summary>
public static class AppThemeNames
{
    public static string DisplayName(this AppTheme theme) => theme switch
    {
        AppTheme.Light => "Claro",
        AppTheme.Dark => "Oscuro",
        _ => theme.ToString(),
    };
}
