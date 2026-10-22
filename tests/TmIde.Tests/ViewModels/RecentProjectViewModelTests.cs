using System;

using TmIde.Services.Settings;
using TmIde.ViewModels.Start;

using Xunit;

namespace TmIde.Tests.ViewModels;

/// <summary>Texto de "última apertura" de los proyectos recientes.</summary>

public sealed class RecentProjectViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0, "hoy")]
    [InlineData(1, "ayer")]
    [InlineData(3, "hace 3 días")]
    [InlineData(8, "hace 1 semana")]
    [InlineData(21, "hace 3 semanas")]
    [InlineData(45, "05/09/2026")]
    public void FormatLastOpened_DescribesHowLongAgo(int daysAgo, string expected)
    {
        Assert.Equal(expected, RecentProjectViewModel.FormatLastOpened(Now.AddDays(-daysAgo), Now));
    }

    [Fact]
    public void FormatLastOpened_EarlierToday_IsToday()
    {
        Assert.Equal("hoy", RecentProjectViewModel.FormatLastOpened(Now.AddHours(-11), Now));
    }

    [Fact]
    public void From_UsesTheFolderNameAsTitle()
    {
        var project = RecentProjectViewModel.From(new RecentProjectEntry(@"C:\proyectos\demo", Now), Now);

        Assert.Equal("demo", project.Name);
        Assert.Equal(@"C:\proyectos\demo", project.Path);
        Assert.Equal("hoy", project.LastOpened);
    }
}
