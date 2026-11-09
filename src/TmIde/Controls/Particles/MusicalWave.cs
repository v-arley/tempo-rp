
using System;
using System.Numerics;

namespace TmIde.Controls.Particles;

/// Deformación temporal de la superficie del orbe provocada por una nota musical.
public sealed class MusicalWave
{
    private const float TransientLifetime = 1.4f;
    private const float ReleaseLifetime = 0.75f;

    private readonly Vector3 _origin;
    private float _age;
    private float _releaseAge = -1f;

    public MusicalWave(int midiNote, bool sustained)
    {
        MidiNote = midiNote;
        IsSustained = sustained;

        // Distribuir las notas alrededor de la esfera.
        // Así distintas notas afectan zonas diferentes.
        var angle = midiNote * 2.39996323f;

        var latitude = Math.Clamp(
            (midiNote - 72) / 60f,
            -0.65f,
            0.65f);

        var horizontalRadius = MathF.Sqrt(
            1f - latitude * latitude);

        _origin = new Vector3(
            horizontalRadius * MathF.Cos(angle),
            latitude,
            horizontalRadius * MathF.Sin(angle));
    }

    public int MidiNote { get; }

    public bool IsSustained { get; }

    public bool IsHeld =>
        IsSustained && _releaseAge < 0f;

    public bool IsAlive =>
        _releaseAge >= 0f
            ? _releaseAge < ReleaseLifetime
            : IsSustained || _age < TransientLifetime;

    /// Comienza el desvanecimiento de una nota sostenida.
    public void Release()
    {
        if (_releaseAge < 0f)
        {
            _releaseAge = 0f;
        }
    }

    public void Advance(float seconds)
    {
        var elapsed = Math.Max(0f, seconds);

        _age += elapsed;

        if (_releaseAge >= 0f)
        {
            _releaseAge += elapsed;
        }
    }

    /// Calcula cuánto debe sobresalir o hundirse un punto de la superficie.
    public float HeightAt(Vector3 direction)
    {
        // Las notas graves crean ondas más amplias;
        // las agudas crean ondas más finas y rápidas.
        var pitch = Math.Clamp(
            (MidiNote - 48) / 48f,
            0f,
            1f);

        var amplitude = 0.14f - 0.045f * pitch;
        var frequency = 5.5f + 10f * pitch;
        var speed = 6f + 12f * pitch;

        // Ataque suave para evitar saltos bruscos.
        var attack = MathF.Min(
            1f,
            _age / 0.06f);

        // Las notas cortas desaparecen naturalmente.
        // Las sostenidas mantienen parte de su energía.
        var decay = IsSustained
            ? 0.55f + 0.45f * MathF.Exp(-_age / 1.5f)
            : MathF.Exp(-_age / 0.38f);

        var release = _releaseAge >= 0f
            ? MathF.Exp(-_releaseAge / 0.22f)
            : 1f;

        // Producto punto: genera patrones de ondas
        // alrededor de la zona donde nace la nota.
        var alignment = Vector3.Dot(
            direction,
            _origin);

        var wave = MathF.Sin(
            frequency * alignment - speed * _age);

        var spatialFalloff =
            0.75f + 0.25f * alignment;

        return amplitude *
               attack *
               decay *
               release *
               wave *
               spatialFalloff;
    }
}
