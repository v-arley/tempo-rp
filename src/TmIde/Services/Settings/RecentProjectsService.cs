using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Extensions.Options;

namespace TmIde.Services.Settings;

/// <summary>
/// <see cref="IRecentProjectsService"/> guardado en las preferencias del
/// usuario. Sin duplicados (la ruta no distingue mayúsculas, como Windows) y
/// con a lo sumo <see cref="IdeOptions.MaxRecentProjects"/> entradas.
/// </summary>

public sealed class RecentProjectsService : IRecentProjectsService
{
    private readonly IUserSettingsStore _store;
    private readonly IOptions<IdeOptions> _options;
    private readonly TimeProvider _time;

    public RecentProjectsService(IUserSettingsStore store, IOptions<IdeOptions> options, TimeProvider time)
    {
        _store = store;
        _options = options;
        _time = time;
    }

    public IReadOnlyList<RecentProjectEntry> GetAll() => _store.Load().RecentProjects;

    public void Add(string projectDirectory)
    {
        var fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(projectDirectory));
        var settings = _store.Load();

        var recents = settings.RecentProjects
            .Where(entry => !string.Equals(entry.Path, fullPath, StringComparison.OrdinalIgnoreCase))
            .Prepend(new RecentProjectEntry(fullPath, _time.GetLocalNow()))
            .Take(Math.Max(0, _options.Value.MaxRecentProjects))
            .ToList();

        _store.Save(settings with { RecentProjects = recents });
    }
}
