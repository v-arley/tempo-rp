using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Tempo.Language.Binding;

namespace TmIde.Controls.Particles;

/// <summary>
/// El mundo del panel: una nube esférica de partículas cuya forma
/// depende del <see cref="Mood"/>, más las chispas de cada <c>pulse</c>.
/// <para>
/// Las ráfagas no salen en cuanto llegan: esperan a la siguiente corchea del
/// <see cref="BeatClock"/>. Un programa que lanza tres <c>pulse</c> seguidos en
/// un microsegundo se ve como un arpegio al ritmo de <c>tempo</c>, no como un
/// único destello.
/// </para>
/// </summary>

public sealed class ParticleSimulation
{
    // Pensado para el panel bajo Output (unos 260 × 160 px): con más puntos la
    // nube se vuelve una mancha y se pierde la textura de puntos.
    public const int DefaultPointCount = 3500;
    public const int MaxSparksPerBurst = 60;
    public const int MaxSparks = 900;
    public const int MaxWaitingBursts = 64;

    // La nube se ve un poco desde arriba y gira despacio sobre sí misma.
    private const float Tilt = 0.35f;
    private const float SpinPerSecond = 0.2f;

    private readonly Vector3[] _surface;
    private readonly Queue<double> _waitingBursts = new();
    private readonly BeatClock _clock = new();
    private readonly MoodMixer _mixer = new();
    private readonly Random _random;
    private List<Spark> _sparks = [];

    public ParticleSimulation(int pointCount = DefaultPointCount, Random? random = null)
    {
        _random = random ?? new Random();
        _surface = FibonacciSphere.Points(pointCount);
    }

    /// <summary>Estado del proyecto; la nube se funde hacia él (<see cref="MoodMixer"/>).</summary>
    public DiagnosticMood Mood
    {
        get => _mixer.Target;
        set => _mixer.Target = value;
    }

    /// <summary>Colores de este instante, mezclados durante una transición.</summary>
    public DotPalette Palette => _mixer.Palette;

    /// <summary>Ritmo al que se cuantizan las ráfagas.</summary>
    public int TempoBpm { get; set; } = TempoLimits.DefaultBpm;

    public int WaitingBursts => _waitingBursts.Count;

    public int SparkCount => _sparks.Count;

    /// <summary>Encola una ráfaga de intensidad (0, 100]; sonará en la siguiente corchea.</summary>
    public void Pulse(double intensity)
    {
        if (_waitingBursts.Count < MaxWaitingBursts)
        {
            _waitingBursts.Enqueue(intensity);
        }
    }

    public void Advance(double seconds)
    {
        var eighthNotes = _clock.Advance(seconds, TempoBpm);
        _mixer.Advance((float)seconds);

        _sparks = _sparks
            .Select(spark => spark.After((float)seconds))
            .Where(spark => spark.IsAlive)
            .ToList();

        for (var note = 0; note < eighthNotes && _waitingBursts.Count > 0; note++)
        {
            Burst(_waitingBursts.Dequeue());
        }
    }

    /// <summary>Posiciones de todas las partículas en este instante, listas para dibujar.</summary>
    public IEnumerable<Vector3> Points()
    {
        var seconds = _clock.Seconds;
        var view = Matrix4x4.CreateRotationY(seconds * SpinPerSecond) * Matrix4x4.CreateRotationX(Tilt);

        return _surface
            .Select(direction => Vector3.Transform(direction * _mixer.RadiusAt(direction, seconds, _random), view))
            .Concat(_sparks.Select(spark => spark.Position));
    }

    // Número de chispas proporcional a la intensidad (0 a 100); siempre al menos una.
    // Salen del centro por direcciones de la propia esfera y la atraviesan.
    private void Burst(double intensity)
    {
        var count = Math.Max(1, (int)Math.Round(intensity / 100 * MaxSparksPerBurst));
        var speed = 1.2f + (float)(intensity / 100) * 1.4f;
        var room = MaxSparks - _sparks.Count;

        _sparks.AddRange(Enumerable.Range(0, Math.Min(count, room)).Select(_ => Spark.FromCenter(RandomSurfaceDirection() * speed * RandomSpread())));
    }

    private Vector3 RandomSurfaceDirection() => _surface[_random.Next(_surface.Length)];

    // Cada chispa sale entre el 60 % y el 100 % de la velocidad de la ráfaga.
    private float RandomSpread() => 0.6f + (float)_random.NextDouble() * 0.4f;
}
