
using System;

using TmIde.Services.Audio;

using Xunit;

namespace TmIde.Tests.Services.Audio;

public sealed class PianoNoteMapTests
{
    [Theory]
    [InlineData('a', 60)]
    [InlineData('s', 62)]
    [InlineData('d', 64)]
    [InlineData('f', 65)]
    [InlineData('g', 67)]
    [InlineData('h', 69)]
    [InlineData('j', 71)]
    [InlineData('k', 72)]
    [InlineData('w', 61)]
    [InlineData('e', 63)]
    [InlineData('t', 66)]
    [InlineData('y', 68)]
    [InlineData('u', 70)]
    [InlineData('ñ', 84)]
    [InlineData('A', 60)]
    [InlineData(' ', 55)]
    [InlineData('0', 60)]
    public void Key_ProducesExpectedMidiNote(
        char key,
        int expectedMidi)
    {
        Assert.True(
            PianoNoteMap.TryGetMidi(key, out var midi));

        Assert.Equal(expectedMidi, midi);
    }

    [Fact]
    public void A4_HasFrequency440Hz()
    {
        var frequency = PianoNoteMap.GetFrequency(69);

        Assert.Equal(440.0, frequency, 5);
    }

    [Fact]
    public void MiddleC_HasCorrectFrequency()
    {
        var frequency = PianoNoteMap.GetFrequency(60);

        Assert.InRange(
            frequency,
            261.62,
            261.64);
    }

    [Fact]
    public void ProgrammingSymbols_ProduceNotes()
    {
        Assert.True(
            PianoNoteMap.TryGetMidi('>', out _));

        Assert.True(
            PianoNoteMap.TryGetMidi('=', out _));

        Assert.True(
            PianoNoteMap.TryGetMidi(';', out _));
    }

    [Fact]
    public void ControlCharacters_DoNotProduceNotes()
    {
        Assert.False(
            PianoNoteMap.TryGetMidi('\n', out _));

        Assert.False(
            PianoNoteMap.TryGetMidi('\t', out _));
    }
}
