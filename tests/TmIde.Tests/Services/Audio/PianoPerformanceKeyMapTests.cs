
using System.Windows.Input;

using TmIde.Services.Audio;

using Xunit;

namespace TmIde.Tests.Services.Audio;

public sealed class PianoPerformanceKeyMapTests
{
    [Theory]
    [InlineData(Key.A, 4, 60)]
    [InlineData(Key.W, 4, 61)]
    [InlineData(Key.S, 4, 62)]
    [InlineData(Key.E, 4, 63)]
    [InlineData(Key.D, 4, 64)]
    [InlineData(Key.F, 4, 65)]
    [InlineData(Key.T, 4, 66)]
    [InlineData(Key.G, 4, 67)]
    [InlineData(Key.Y, 4, 68)]
    [InlineData(Key.H, 4, 69)]
    [InlineData(Key.U, 4, 70)]
    [InlineData(Key.J, 4, 71)]
    [InlineData(Key.K, 4, 72)]
    [InlineData(Key.O, 4, 73)]
    [InlineData(Key.L, 4, 74)]
    [InlineData(Key.P, 4, 75)]
    public void Keyboard_ProducesExpectedNote(
        Key key,
        int octave,
        int expectedMidi)
    {
        var found = PianoPerformanceKeyMap.TryGetMidi(
            key,
            octave,
            out var midiNote);

        Assert.True(found);
        Assert.Equal(expectedMidi, midiNote);
    }

    [Fact]
    public void ChangingOctave_ChangesNoteByTwelveSemitones()
    {
        Assert.True(
            PianoPerformanceKeyMap.TryGetMidi(
                Key.A, 4, out var middleC));

        Assert.True(
            PianoPerformanceKeyMap.TryGetMidi(
                Key.A, 5, out var higherC));

        Assert.Equal(12, higherC - middleC);
    }

    [Theory]
    [InlineData(Key.Z)]
    [InlineData(Key.X)]
    [InlineData(Key.Escape)]
    [InlineData(Key.Space)]
    [InlineData(Key.Enter)]
    public void ControlKeys_AreNotMusicalNotes(Key key)
    {
        Assert.False(
            PianoPerformanceKeyMap.TryGetMidi(
                key,
                4,
                out _));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    public void OutOfRangeOctave_IsRejected(int octave)
    {
        Assert.False(
            PianoPerformanceKeyMap.TryGetMidi(
                Key.A,
                octave,
                out _));
    }
}
