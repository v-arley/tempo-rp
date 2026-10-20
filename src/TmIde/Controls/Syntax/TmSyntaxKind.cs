namespace TmIde.Controls.Syntax;

/// <summary>Categoría de color de un fragmento de código TM.</summary>
public enum TmSyntaxKind
{
    Keyword,
    Type,

    /// <summary>Valores predefinidos del lenguaje: <c>true</c>, <c>false</c>, <c>silence</c>.</summary>
    BuiltInValue,
    Number,

    /// <summary>Cadena <c>"…"</c> o carácter <c>'…'</c>.</summary>
    StringLiteral,
    Comment,
}
