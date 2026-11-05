using System.Linq;

using TmIde.Services.Execution;

using Xunit;

namespace TmIde.Tests.Services;

/// <summary>Buzón de ráfagas entre el intérprete y el panel de partículas.</summary>

public sealed class IdeRuntimePulseTests
{
    private readonly IdeRuntimePulse _pulse = new();

    [Fact]
    public void TakeBursts_ReturnsInArrivalOrder_AndEmptiesTheInbox()
    {
        _pulse.Emit(40);
        _pulse.Emit(44);
        _pulse.Emit(47);

        Assert.Equal([40, 44, 47], _pulse.TakeBursts());
        Assert.Empty(_pulse.TakeBursts());
    }

    [Fact]
    public void Emit_BeyondCapacity_DropsTheRest()
    {
        foreach (var intensity in Enumerable.Range(1, IdeRuntimePulse.Capacity + 50))
        {
            _pulse.Emit(intensity);
        }

        Assert.Equal(IdeRuntimePulse.Capacity, _pulse.TakeBursts().Count);
    }
}
