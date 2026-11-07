
using System;
using System.Collections.Generic;

namespace TmIde.Services.Audio;

/// <summary>
/// Traduce caracteres escritos a notas MIDI.
/// Esta distribución también servirá como punto
/// de partida para el futuro modo de interpretación.
/// </summary>
public static class PianoNoteMap
{
    private static readonly Dictionary<char, int> Letters = new()
    {
        // Fila central: notas naturales.
        ['a'] = 60, // Do4
        ['s'] = 62, // Re4
        ['d'] = 64, // Mi4
        ['f'] = 65, // Fa4
        ['g'] = 67, // Sol4
        ['h'] = 69, // La4
        ['j'] = 71, // Si4
        ['k'] = 72, // Do5
        ['l'] = 74, // Re5

        // Notas sostenidas.
        ['w'] = 61, // Do#4
        ['e'] = 63, // Re#4
        ['t'] = 66, // Fa#4
        ['y'] = 68, // Sol#4
        ['u'] = 70, // La#4

        // Resto de letras: otras notas y octavas.
        ['q'] = 48,
        ['r'] = 50,
        ['i'] = 52,
        ['o'] = 53,
        ['p'] = 55,

        ['z'] = 57,
        ['x'] = 59,
        ['c'] = 76,
        ['v'] = 77,
        ['b'] = 79,
        ['n'] = 81,
        ['m'] = 83,
        ['ñ'] = 84,
    };

    /// <summary>
    /// Obtiene una nota MIDI para el carácter escrito.
    /// </summary>
    public static bool TryGetMidi(char character, out int midiNote)
    {
        if (Letters.TryGetValue(
            char.ToLowerInvariant(character),
            out midiNote))
        {
            return true;
        }

        // Espacio: nota grave y suave.
        if (character == ' ')
        {
            midiNote = 55;
            return true;
        }

        // Dígitos del 0 al 9.
        if (character is >= '0' and <= '9')
        {
            midiNote = 60 + character - '0';
            return true;
        }

        // Operadores y signos usados al programar.
        if (char.IsPunctuation(character) ||
            char.IsSymbol(character))
        {
            midiNote = 72 + character % 12;
            return true;
        }

        midiNote = 0;
        return false;
    }

    /// <summary>
    /// Convierte una nota MIDI a frecuencia en Hz.
    /// La4 = MIDI 69 = 440 Hz.
    /// </summary>
    public static double GetFrequency(int midiNote)
    {
        return 440.0 * Math.Pow(
            2.0,
            (midiNote - 69) / 12.0);
    }
}
