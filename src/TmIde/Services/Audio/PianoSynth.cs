
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace TmIde.Services.Audio;

/// Sintetizador polifónico de Tempo.
/// Soporta notas cortas y notas sostenidas.
public sealed class PianoSynth : IDisposable
{
    private const int SampleRate = 44100;

    private readonly WaveFormat _format =
        WaveFormat.CreateIeeeFloatWaveFormat(
            SampleRate, 1);

    // Teclas físicas que permanecen presionadas.
    private readonly Dictionary<int, PianoVoice>
        _heldNotes = new();

    private MixingSampleProvider? _mixer;
    private WaveOutEvent? _output;

    /// <summary>
    /// Reproduce una nota corta para escritura musical.
    /// </summary>
    public bool TryPlay(int midiNote)
    {
        try
        {
            EnsureAudio();

            _mixer!.AddMixerInput(
                new PianoVoice(
                    midiNote,
                    _format,
                    sustained: false));

            _output!.Play();
            return true;
        }
        catch (Exception exception)
        {
            HandleAudioError(exception);
            return false;
        }
    }

    /// <summary>
    /// Inicia una nota que seguirá sonando mientras
    /// su tecla permanezca presionada.
    /// </summary>
    public bool TryStartSustainedNote(
        int keyId,
        int midiNote)
    {
        // No repetir una nota si la tecla sigue pulsada.
        if (_heldNotes.ContainsKey(keyId))
        {
            return true;
        }

        try
        {
            EnsureAudio();

            var voice = new PianoVoice(
                midiNote,
                _format,
                sustained: true);

            _mixer!.AddMixerInput(voice);
            _heldNotes.Add(keyId, voice);

            _output!.Play();
            return true;
        }
        catch (Exception exception)
        {
            HandleAudioError(exception);
            return false;
        }
    }

    /// <summary>
    /// Comprueba si una tecla tiene una nota activa.
    /// </summary>
    public bool IsSustainedNoteActive(int keyId)
    {
        return _heldNotes.ContainsKey(keyId);
    }

    /// <summary>
    /// Solicita que una nota termine suavemente.
    /// </summary>
    public void ReleaseSustainedNote(int keyId)
    {
        if (_heldNotes.Remove(keyId, out var voice))
        {
            voice.Release();
        }
    }

    /// <summary>
    /// Libera las teclas musicales activas.
    /// Las notas terminan con un desvanecimiento.
    /// </summary>
    public void ReleaseAllSustainedNotes()
    {
        foreach (var voice in _heldNotes.Values)
        {
            voice.Release();
        }

        _heldNotes.Clear();
    }

    /// <summary>
    /// Detiene inmediatamente todos los sonidos.
    /// Se usa al desactivar el piano o cambiar de modo.
    /// </summary>
    public void Stop()
    {
        _heldNotes.Clear();
        _mixer?.RemoveAllMixerInputs();
        _output?.Pause();
    }

    private void EnsureAudio()
    {
        if (_output is not null)
        {
            return;
        }

        _mixer = new MixingSampleProvider(_format)
        {
            ReadFully = true
        };

        _output = new WaveOutEvent
        {
            DesiredLatency = 80
        };

        _output.Init(
            new SampleToWaveProvider(_mixer));
    }

    private void HandleAudioError(Exception exception)
    {
        Debug.WriteLine(
            $"Error de audio de Tempo: {exception}");

        Dispose();
    }

    public void Dispose()
    {
        _heldNotes.Clear();
        _output?.Stop();
        _output?.Dispose();

        _output = null;
        _mixer = null;
    }

    /// <summary>
    /// Voz individual del sintetizador.
    /// La nota sostenida tiene tres momentos:
    /// ataque, sostenimiento y liberación.
    /// </summary>
    private sealed class PianoVoice : ISampleProvider
    {
        private const double OneShotSeconds = 0.34;
        private const double ReleaseSeconds = 0.16;

        private readonly double _frequency;
        private readonly bool _sustained;
        private readonly int _oneShotSamples;
        private readonly int _releaseSamples;

        private long _sampleIndex;
        private long _releaseStart = -1;

        // Esta bandera se comparte entre el hilo de
        // interfaz y el hilo que genera el audio.
        private int _releaseRequested;

        public PianoVoice(
            int midiNote,
            WaveFormat format,
            bool sustained)
        {
            WaveFormat = format;
            _frequency =
                PianoNoteMap.GetFrequency(midiNote);

            _sustained = sustained;

            _oneShotSamples = (int)(
                format.SampleRate * OneShotSeconds);

            _releaseSamples = (int)(
                format.SampleRate * ReleaseSeconds);
        }

        public WaveFormat WaveFormat { get; }

        /// <summary>
        /// Indica que la tecla fue liberada.
        /// El hilo de audio realizará el desvanecimiento.
        /// </summary>
        public void Release()
        {
            Volatile.Write(
                ref _releaseRequested, 1);
        }

        public int Read(
            float[] buffer,
            int offset,
            int count)
        {
            var written = 0;
            var sampleRate = WaveFormat.SampleRate;

            while (written < count)
            {
                if (_sustained &&
                    Volatile.Read(ref _releaseRequested) != 0 &&
                    _releaseStart < 0)
                {
                    _releaseStart = _sampleIndex;
                }

                // Terminar la nota corta.
                if (!_sustained &&
                    _sampleIndex >= _oneShotSamples)
                {
                    break;
                }

                // Terminar la nota sostenida
                // después del desvanecimiento.
                if (_sustained &&
                    _releaseStart >= 0 &&
                    _sampleIndex - _releaseStart >=
                    _releaseSamples)
                {
                    break;
                }

                var time =
                    _sampleIndex / (double)sampleRate;

                // Ataque rápido para evitar chasquidos.
                var attack =
                    1.0 - Math.Exp(-time * 380.0);

                // Las notas cortas decaen rápidamente.
                // Las sostenidas conservan parte del
                // volumen mientras se mantiene la tecla.
                var decay = _sustained
                    ? 0.38 + 0.62 * Math.Exp(-time * 3.0)
                    : Math.Exp(-time * 7.0);

                var fade = 1.0;

                if (_sustained && _releaseStart >= 0)
                {
                    var progress =
                        (_sampleIndex - _releaseStart) /
                        (double)_releaseSamples;

                    // Liberación gradual y suave.
                    var remaining =
                        Math.Max(0.0, 1.0 - progress);

                    fade = remaining * remaining;
                }
                else if (!_sustained)
                {
                    // Pequeño fundido al final de
                    // las notas del modo escritura.
                    var remaining =
                        _oneShotSamples - _sampleIndex;

                    fade = Math.Min(
                        1.0,
                        remaining / (sampleRate * 0.04));
                }

                // Frecuencia fundamental y armónicos.
                var phase =
                    2.0 * Math.PI * _frequency * time;

                var sound =
                    0.62 * Math.Sin(phase) +
                    0.28 * Math.Sin(phase * 2.0) +
                    0.10 * Math.Sin(phase * 3.0);

                buffer[offset + written] = (float)(
                    sound *
                    attack *
                    decay *
                    fade *
                    0.055);

                _sampleIndex++;
                written++;
            }

            // Cuando devuelve menos muestras de las
            // solicitadas, NAudio puede retirar la voz
            // del mezclador al finalizar.
            return written;
        }
    }
}
