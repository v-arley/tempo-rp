
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using TmIde.Services.Settings;
using System.Windows.Input;

namespace TmIde.Services.Audio;

/// <summary>
/// Información de una nota que acaba de sonar.
/// También servirá para animar el orbe.
/// </summary>
public sealed class PianoNotePlayedEventArgs : EventArgs
{
    public PianoNotePlayedEventArgs(int midiNote)
    {
        MidiNote = midiNote;
        Frequency = PianoNoteMap.GetFrequency(midiNote);
    }

    public int MidiNote { get; }

    public double Frequency { get; }
}

/// <summary>
/// Controla el piano de Tempo y sus dos modalidades.
/// </summary>
public sealed class TypingPianoService : ObservableObject, IDisposable
{
    private readonly IUserSettingsStore _settings;
    private readonly PianoSynth _synth = new();

    private bool _enabled;
    private bool _isPerformanceMode;
    private int _octave;

    private static readonly int[] EnterChord =
    [
        60, // Do4
        64, // Mi4
        67  // Sol4
    ];

    public TypingPianoService(IUserSettingsStore settings)
    {
        _settings = settings;

        var saved = settings.Load();

        _enabled = saved.PianoEnabled;
        _isPerformanceMode = saved.PianoPerformanceMode;
        _octave = Math.Clamp(saved.PianoOctave, 2, 6);
    }

    /// <summary>
    /// Activa o desactiva el sonido.
    /// </summary>
    public bool Enabled
    {
        get => _enabled;

        set
        {
            if (!SetProperty(ref _enabled, value))
            {
                return;
            }

            _settings.Save(
                _settings.Load() with
                {
                    PianoEnabled = value
                });

            if (!value)
            {
                _synth.Stop();
            }
        }
    }

    /// <summary>
    /// False: notas al escribir.
    /// True: teclado convertido en instrumento.
    /// </summary>
    public bool IsPerformanceMode
    {
        get => _isPerformanceMode;

        set
        {
            if (!SetProperty(ref _isPerformanceMode, value))
            {
                return;
            }

            // Detener sonidos previos al cambiar de modo.
            _synth.Stop();

            OnPropertyChanged(nameof(ToggleLabel));

            _settings.Save(
                _settings.Load() with
                {
                    PianoPerformanceMode = value
                });
        }
    }

    /// <summary>
    /// Octava del modo de interpretación (2 a 6).
    /// </summary>
    public int Octave
    {
        get => _octave;

        set
        {
            var next = Math.Clamp(value, 2, 6);

            if (!SetProperty(ref _octave, next))
            {
                return;
            }

            ReleaseAllPerformanceNotes();

            OnPropertyChanged(nameof(OctaveLabel));

            _settings.Save(
                _settings.Load() with
                {
                    PianoOctave = next
                });
        }
    }

    /// <summary>
    /// Texto que aparece en el toggle superior.
    /// </summary>
    public string ToggleLabel =>
        IsPerformanceMode ? "♫ TOCAR" : "♫ PIANO";

    public string OctaveLabel => $"Octava actual: {Octave}";

    public event EventHandler<PianoNotePlayedEventArgs>? NotePlayed;

    public void ChangeOctave(int difference)
    {
        Octave += difference;
    }

    /// <summary>
    /// Notas de la modalidad de escritura.
    /// </summary>
    public void PlayCharacter(char character)
    {
        if (!Enabled || IsPerformanceMode ||
            !PianoNoteMap.TryGetMidi(character, out var note))
        {
            return;
        }

        PlayNote(note);
    }

    public void PlayEnter()
    {
        if (!Enabled || IsPerformanceMode)
        {
            return;
        }

        foreach (var note in EnterChord)
        {
            PlayNote(note);
        }
    }

    public void PlayBackspace()
    {
        if (Enabled && !IsPerformanceMode)
        {
            PlayNote(48);
        }
    }

    
    /// Comienza a reproducir una nota musical al presionar una tecla del modo interpretación.
    public void StartPerformanceNote(
        Key key,
        int midiNote)
    {
        if (!Enabled || !IsPerformanceMode)
        {
            return;
        }

        var keyId = (int)key;

        // La nota no debe reiniciarse si el sistema
        // repite la tecla mientras sigue presionada.
        if (_synth.IsSustainedNoteActive(keyId))
        {
            return;
        }

        if (!_synth.TryStartSustainedNote(
            keyId, midiNote))
        {
            Enabled = false;
            return;
        }

        // El evento sigue disponible para el futuro
        // visualizador musical del orbe.
        NotePlayed?.Invoke(
            this,
            new PianoNotePlayedEventArgs(midiNote));
    }

    /// Libera únicamente la nota de esta tecla.
    public void StopPerformanceNote(Key key)
    {
        _synth.ReleaseSustainedNote((int)key);
    }

    /// Libera todas las notas del modo interpretación.
    /// Útil cuando el editor pierde el foco.
    public void ReleaseAllPerformanceNotes()
    {
        _synth.ReleaseAllSustainedNotes();
    }


    /// Reproduce una nota del teclado musical.
    public void PlayPerformanceNote(int midiNote)
    {
        if (!Enabled || !IsPerformanceMode)
        {
            return;
        }

        PlayNote(midiNote);
    }

    private void PlayNote(int midiNote)
    {
        if (!Enabled)
        {
            return;
        }

        if (!_synth.TryPlay(midiNote))
        {
            // Si el audio falla, el IDE sigue funcionando.
            Enabled = false;
            return;
        }

        NotePlayed?.Invoke(
            this,
            new PianoNotePlayedEventArgs(midiNote));
    }

    public void Dispose()
    {
        _synth.Dispose();
    }
}
