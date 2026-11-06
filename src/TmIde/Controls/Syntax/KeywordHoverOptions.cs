namespace TmIde.Controls.Syntax;

/// Configuración de la ayuda contextual de palabras reservadas
public static class KeywordHoverOptions
{
    // true  = requiere Ctrl + Hover
    // false = basta con pasar el mouse sobre la palabra
    public static bool RequireControlKey { get; set; } = false;
}