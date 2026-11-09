using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using TmIde.Services;

using Xunit;

namespace TmIde.Tests.Services;

/// <summary>
/// Contrato entre temas: cada <see cref="AppTheme"/> tiene su
/// <c>Resources/Theme.&lt;nombre&gt;.xaml</c> y define exactamente las mismas
/// claves que el tema claro. Una clave ausente no falla en WPF: deja la zona sin color.
/// </summary>
public sealed class ThemeCatalogTests
{
    private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

    public static TheoryData<AppTheme> Themes() => new(Enum.GetValues<AppTheme>());

    [Theory]
    [MemberData(nameof(Themes))]
    public void EveryTheme_DefinesTheSameKeysAsTheLightTheme(AppTheme theme)
    {
        var expected = KeysOf(AppTheme.Light);
        var actual = KeysOf(theme);

        Assert.Empty(expected.Except(actual));
        Assert.Empty(actual.Except(expected));
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void EveryTheme_HasADisplayName(AppTheme theme) =>
        Assert.NotEqual(theme.ToString(), theme.DisplayName());

    private static string[] KeysOf(AppTheme theme) =>
        XDocument.Load(ThemeFileOf(theme))
            .Root!
            .Elements()
            .Select(element => (string?)element.Attribute(XamlNamespace + "Key"))
            .OfType<string>()
            .ToArray();

    // Sube desde la carpeta de salida de los tests hasta encontrar el código fuente del IDE.
    private static string ThemeFileOf(AppTheme theme)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "src", "TmIde", "Resources", $"Theme.{theme}.xaml");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException($"No se encontró Theme.{theme}.xaml en src/TmIde/Resources.");
    }
}
