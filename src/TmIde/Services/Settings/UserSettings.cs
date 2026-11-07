using System;
using System.Collections.Generic;

namespace TmIde.Services.Settings;

/// <summary>
/// Preferencias del usuario que sobreviven entre sesiones. Inmutable: se
/// modifica con <c>with</c> y se guarda con <see cref="IUserSettingsStore"/>.
/// </summary>

public sealed record UserSettings
{
    /// <summary>Tema elegido; <c>null</c> hasta que el usuario cambie el de <see cref="IdeOptions.DefaultTheme"/>.</summary>
    public AppTheme? Theme { get; init; }


    /// Indica si el piano del teclado está activado.
    /// Desactivado por defecto.
    public bool PianoEnabled { get; init; } 

    /// <summary>Proyectos abiertos, el más reciente primero.</summary>
    public IReadOnlyList<RecentProjectEntry> RecentProjects { get; init; } = [];
}

/// <summary>Un proyecto abierto y cuándo se abrió por última vez.</summary>

public sealed record RecentProjectEntry(string Path, DateTimeOffset LastOpened);
