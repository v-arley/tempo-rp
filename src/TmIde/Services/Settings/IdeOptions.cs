namespace TmIde.Services.Settings;

/// <summary>
/// Configuración de la aplicación, de solo lectura: sección <c>Ide</c> de
/// <c>appsettings.json</c>. Lo que el usuario cambia mientras usa el IDE va en
/// <see cref="UserSettings"/>.
/// </summary>

public sealed class IdeOptions
{
    public const string SectionName = "Ide";

    /// <summary>Cuántos proyectos recientes se recuerdan.</summary>
    public int MaxRecentProjects { get; set; } = 10;

    /// <summary>Tema hasta que el usuario elija otro.</summary>
    public AppTheme DefaultTheme { get; set; } = AppTheme.Light;
}
