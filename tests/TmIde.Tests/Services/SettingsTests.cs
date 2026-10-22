using System;
using System.IO;
using System.Linq;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using TmIde.Services;
using TmIde.Services.Settings;

using Xunit;

namespace TmIde.Tests.Services;

/// <summary>
/// Preferencias del usuario: el archivo JSON (<see cref="JsonUserSettingsStore"/>)
/// y el historial de recientes (<see cref="RecentProjectsService"/>) sobre él.
/// </summary>

public sealed class SettingsTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly TempDirectory _directory = TempDirectory.Create();
    private readonly FixedTimeProvider _time = new(Now);

    public void Dispose() => _directory.Dispose();

    [Fact]
    public void Load_NoFile_ReturnsDefaults()
    {
        var settings = CreateStore().Load();

        Assert.Null(settings.Theme);
        Assert.Empty(settings.RecentProjects);
    }

    [Fact]
    public void SaveThenLoad_KeepsThemeAndRecents()
    {
        var store = CreateStore();
        var saved = new UserSettings
        {
            Theme = AppTheme.Dark,
            RecentProjects = [new RecentProjectEntry(@"C:\proyectos\demo", Now)],
        };

        store.Save(saved);
        var loaded = store.Load();

        Assert.Equal(AppTheme.Dark, loaded.Theme);
        Assert.Equal(saved.RecentProjects, loaded.RecentProjects);
    }

    [Fact]
    public void Save_WritesTheThemeAsText()
    {
        CreateStore().Save(new UserSettings { Theme = AppTheme.Dark });

        Assert.Contains("\"Dark\"", File.ReadAllText(SettingsPath), StringComparison.Ordinal);
    }

    [Fact]
    public void Load_CorruptFile_ReturnsDefaults()
    {
        File.WriteAllText(SettingsPath, "{ esto no es json");

        var settings = CreateStore().Load();

        Assert.Null(settings.Theme);
        Assert.Empty(settings.RecentProjects);
    }

    [Fact]
    public void Add_PutsTheProjectFirstWithTheCurrentTime()
    {
        var recents = CreateRecents();
        recents.Add(_directory.CreateDirectory("a"));
        _time.Now = Now.AddHours(1);

        recents.Add(_directory.CreateDirectory("b"));

        var entries = recents.GetAll();
        Assert.Equal(["b", "a"], entries.Select(entry => Path.GetFileName(entry.Path)));
        Assert.Equal(Now.AddHours(1), entries[0].LastOpened);
    }

    [Fact]
    public void Add_SameProjectAgain_MovesItToTheTopWithoutDuplicates()
    {
        var recents = CreateRecents();
        var a = _directory.CreateDirectory("a");
        recents.Add(a);
        recents.Add(_directory.CreateDirectory("b"));

        recents.Add(a.ToUpperInvariant() + Path.DirectorySeparatorChar);

        Assert.Equal(["a", "b"], recents.GetAll().Select(entry => Path.GetFileName(entry.Path).ToLowerInvariant()));
    }

    [Fact]
    public void Add_BeyondTheLimit_DropsTheOldest()
    {
        var recents = CreateRecents(maxRecentProjects: 2);

        recents.Add(_directory.CreateDirectory("a"));
        recents.Add(_directory.CreateDirectory("b"));
        recents.Add(_directory.CreateDirectory("c"));

        Assert.Equal(["c", "b"], recents.GetAll().Select(entry => Path.GetFileName(entry.Path)));
    }

    [Fact]
    public void Add_KeepsTheSavedTheme()
    {
        var store = CreateStore();
        store.Save(new UserSettings { Theme = AppTheme.Dark });

        CreateRecents(store).Add(_directory.CreateDirectory("a"));

        Assert.Equal(AppTheme.Dark, store.Load().Theme);
    }

    private string SettingsPath => Path.Combine(_directory.Path, "settings.json");

    private JsonUserSettingsStore CreateStore() =>
        new(SettingsPath, NullLogger<JsonUserSettingsStore>.Instance);

    private RecentProjectsService CreateRecents(int maxRecentProjects = 10) =>
        CreateRecents(CreateStore(), maxRecentProjects);

    private RecentProjectsService CreateRecents(IUserSettingsStore store, int maxRecentProjects = 10) =>
        new(store, Options.Create(new IdeOptions { MaxRecentProjects = maxRecentProjects }), _time);
}
