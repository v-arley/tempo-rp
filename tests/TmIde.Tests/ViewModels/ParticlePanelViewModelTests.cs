using CommunityToolkit.Mvvm.Messaging;

using Tempo.Language.Binding;

using TmIde.Controls.Particles;
using TmIde.Services.Execution;
using TmIde.ViewModels;
using TmIde.ViewModels.Particles;

using Xunit;

namespace TmIde.Tests.ViewModels;

/// <summary>
/// El panel de partículas escucha el análisis del proyecto sin conocer a quien
/// lo produce.
/// </summary>

public sealed class ParticlePanelViewModelTests
{
    private readonly IMessenger _messenger = new WeakReferenceMessenger();
    private readonly IdeRuntimePulse _runtimePulse = new();
    private readonly ParticlePanelViewModel _panel;

    public ParticlePanelViewModelTests()
    {
        _panel = new ParticlePanelViewModel(_runtimePulse, _messenger);
    }

    [Fact]
    public void Starts_CalmAtDefaultTempo()
    {
        Assert.Equal(DiagnosticMood.Correct, _panel.Mood);
        Assert.Equal(TempoLimits.DefaultBpm, _panel.TempoBpm);
    }

    [Fact]
    public void ProjectAnalyzed_UpdatesMoodAndTempo()
    {
        var mood = DiagnosticMood.From(errors: 2, warnings: 0);

        _messenger.Send(new ProjectAnalyzedMessage(mood, TempoBpm: 96));

        Assert.Equal(mood, _panel.Mood);
        Assert.Equal(96, _panel.TempoBpm);
    }

    [Fact]
    public void TakeBursts_ReadsTheSharedInbox()
    {
        _runtimePulse.Emit(55);

        Assert.Equal([55], _panel.TakeBursts());
    }
}
