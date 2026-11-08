
using System.Collections.Generic;
using System.Windows.Input;

namespace TmIde.Services.Audio;

/// <summary>
/// Distribución del teclado para interpretar música.
/// Los valores representan semitonos desde Do.
/// </summary>
public static class PianoPerformanceKeyMap
{
    private static readonly Dictionary<Key, int> Semitones = new()
    {
        [Key.A] = 0,  // Do
        [Key.W] = 1,  // Do#
        [Key.S] = 2,  // Re
        [Key.E] = 3,  // Re#
        [Key.D] = 4,  // Mi
        [Key.F] = 5,  // Fa
        [Key.T] = 6,  // Fa#
        [Key.G] = 7,  // Sol
        [Key.Y] = 8,  // Sol#
        [Key.H] = 9,  // La
        [Key.U] = 10, // La#
        [Key.J] = 11, // Si
        [Key.K] = 12, // Do siguiente

        // Continuación hacia notas más agudas.
        [Key.O] = 13, // Do#
        [Key.L] = 14, // Re
        [Key.P] = 15  // Re#
    };

    public static bool TryGetMidi(
        Key key,
        int octave,
        out int midiNote)
    {
        if (octave < 2 || octave > 6 ||
            !Semitones.TryGetValue(key, out var semitone))
        {
            midiNote = 0;
            return false;
        }

        // Do4 = nota MIDI 60.
        midiNote = 12 * (octave + 1) + semitone;
        return true;
    }
}
