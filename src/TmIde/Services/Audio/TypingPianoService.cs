
using System;

using CommunityToolkit.Mvvm.ComponentModel;

using TmIde.Services.Settings;

namespace TmIde.Services.Audio;

/// <summary>
/// Información de una nota reproducida.
/// Más adelante podrá consumirla el visualizador.
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
/// Controla el modo piano del editor.
/// </summary>
public sealed class TypingPianoService : ObservableObject, IDisposable
{
    private readonly IUserSettingsStore _settings;
    private readonly PianoSynth _synth = new();

    private bool _enabled;

    private static readonly int[] EnterChord =
    [
        60, // Do4
        64, // Mi4
        67  // Sol4
    ];

    public TypingPianoService(IUserSettingsStore settings)
    {
        _settings = settings;
        _enabled = settings.Load().PianoEnabled;
    }

    /// <summary>
    /// Permite activar o desactivar el sonido.
    /// La propiedad se enlaza con el toggle de la barra.
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

            // Guardar preferencia.
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
    /// Evento preparado para las futuras
    /// reacciones musicales del orbe.
    /// </summary>
    public event EventHandler<PianoNotePlayedEventArgs>? NotePlayed;

    /// <summary>
    /// Reproduce la nota de un carácter escrito.
    /// </summary>
    public void PlayCharacter(char character)
    {
        if (!Enabled ||
            !PianoNoteMap.TryGetMidi(character, out var note))
        {
            return;
        }

        PlayNote(note);
    }

    /// <summary>
    /// Enter reproduce un acorde de Do mayor.
    /// </summary>
    public void PlayEnter()
    {
        if (!Enabled)
        {
            return;
        }

        foreach (var note in EnterChord)
        {
            PlayNote(note);
        }
    }

    /// <summary>
    /// Backspace reproduce una nota grave.
    /// </summary>
    public void PlayBackspace()
    {
        if (Enabled)
        {
            PlayNote(48);
        }
    }

    private void PlayNote(int midiNote)
    {
        if (!Enabled)
        {
            return;
        }

        if (!_synth.TryPlay(midiNote))
        {
            // Si no existe dispositivo de audio disponible,
            // desactivar el modo sin cerrar Tempo.
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
