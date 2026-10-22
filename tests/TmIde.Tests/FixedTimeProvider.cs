using System;

namespace TmIde.Tests;

/// <summary>Reloj fijo en UTC para pruebas que dependen de la hora.</summary>

public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => Now;
}
