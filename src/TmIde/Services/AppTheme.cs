namespace TmIde.Services;

/// <summary>
/// Temas disponibles. Cada valor corresponde a un ResourceDictionary en
/// <c>Resources/Theme.&lt;nombre&gt;.xaml</c> con las mismas claves de pincel.
/// Añadir un tema es añadir un valor aquí, su archivo XAML y su nombre en
/// <see cref="AppThemeNames"/>; el menú VER lo lista solo.
/// </summary>

public enum AppTheme
{
    Light,
    Dark,
}
