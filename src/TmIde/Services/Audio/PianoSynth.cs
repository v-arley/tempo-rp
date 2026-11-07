
using System;
using System.Diagnostics;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace TmIde.Services.Audio;

/// <summary>
/// Sintetizador polifónico sencillo.
/// Permite reproducir varias notas simultáneamente.
/// </summary>
public sealed class PianoSynth : IDisposable
{
    private const int SampleRate = 44100;

    private readonly WaveFormat _format =
        WaveFormat.CreateIeeeFloatWaveFormat(
            SampleRate,
            1);

    private MixingSampleProvider? _mixer;
    private WaveOutEvent? _output;

    public bool TryPlay(int midiNote)
    {
        try
        {
            EnsureAudio();

            if (_mixer is null || _output is null)
            {
                return false;
            }

            // Cada nota tiene su propia voz.
            // Pueden sonar varias al mismo tiempo.
            _mixer.AddMixerInput(
                new PianoVoice(midiNote, _format));

            _output.Play();

            return true;
        }
        catch (Exception exception)
        {
            // Un problema de audio no debe cerrar el IDE.
            Debug.WriteLine(
                $"Error de audio de Tempo: {exception}");

            Dispose();

            return false;
        }
    }

    private void EnsureAudio()
    {
        if (_output is not null)
        {
            return;
        }

        _mixer = new MixingSampleProvider(_format)
        {
            // El dispositivo sigue activo aunque
            // no haya notas en ese instante.
            ReadFully = true
        };

        _output = new WaveOutEvent
        {
            DesiredLatency = 80
        };

        _output.Init(
            new SampleToWaveProvider(_mixer));
    }

    /// <summary>
    /// Detiene las notas activas al apagar el modo piano.
    /// </summary>
    public void Stop()
    {
        _mixer?.RemoveAllMixerInputs();
        _output?.Pause();
    }

    public void Dispose()
    {
        _output?.Stop();
        _output?.Dispose();

        _output = null;
        _mixer = null;
    }

    /// <summary>
    /// Una nota sintetizada de duración limitada.
    /// </summary>
    private sealed class PianoVoice : ISampleProvider
    {
        private readonly double _frequency;
        private readonly int _totalSamples;
        private int _sampleIndex;

        public PianoVoice(
            int midiNote,
            WaveFormat format)
        {
            WaveFormat = format;
            _frequency = PianoNoteMap.GetFrequency(midiNote);

            // Nota corta para no saturar al escribir.
            _totalSamples = (int)(
                format.SampleRate * 0.34);
        }

        public WaveFormat WaveFormat { get; }

        public int Read(
            float[] buffer,
            int offset,
            int count)
        {
            var written = 0;

            while (written < count &&
                   _sampleIndex < _totalSamples)
            {
                var time = _sampleIndex /
                    (double)WaveFormat.SampleRate;

                // Ataque corto y decaimiento gradual.
                var attack = 1.0 - Math.Exp(-time * 450);
                var decay = Math.Exp(-time * 7);

                // Fundido al final de cada nota.
                var remaining = _totalSamples - _sampleIndex;

                var release = Math.Min(
                    1.0,
                    remaining /
                    (WaveFormat.SampleRate * 0.04));

                var phase =
                    2.0 * Math.PI * _frequency * time;

                // Armónicos para un timbre más rico
                // que una onda sinusoidal pura.
                var sound =
                    0.62 * Math.Sin(phase) +
                    0.28 * Math.Sin(phase * 2) +
                    0.10 * Math.Sin(phase * 3);

                // Volumen moderado de inicio.
                buffer[offset + written] = (float)(
                    sound * attack * decay *
                    release * 0.055);

                _sampleIndex++;
                written++;
            }

            // Cuando termina, el mezclador elimina
            // automáticamente esta voz.
            return written;
        }
    }
}
