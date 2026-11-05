using System.Collections.Concurrent;
using System.Collections.Generic;

using Tempo.Language.Evaluation;

namespace TmIde.Services.Execution;

/// <summary>
/// Buzón de las ráfagas de <c>pulse</c>: el intérprete deja cada una sin
/// esperar a la UI, y el panel de partículas las recoge en su siguiente
/// fotograma. A diferencia de <see cref="IdeRuntimeOutput"/>, no frena al
/// programa: una ráfaga perdida no cambia el resultado.
/// </summary>

public sealed class IdeRuntimePulse : IRuntimePulse
{
    /// <summary>Ráfagas que caben sin recoger; un bucle desbocado descarta el resto.</summary>
    public const int Capacity = 256;

    private readonly ConcurrentQueue<double> _bursts = new();

    public void Emit(double intensity)
    {
        if (_bursts.Count < Capacity)
        {
            _bursts.Enqueue(intensity);
        }
    }

    /// <summary>Vacía el buzón y devuelve las ráfagas en el orden en que llegaron.</summary>
    public IReadOnlyList<double> TakeBursts()
    {
        var bursts = new List<double>();

        while (_bursts.TryDequeue(out var intensity))
        {
            bursts.Add(intensity);
        }

        return bursts;
    }
}
