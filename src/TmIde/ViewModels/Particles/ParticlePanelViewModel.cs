
using System.Collections.Concurrent;
using System.Collections.Generic;

using CommunityToolkit.Mvvm.Messaging;

using Tempo.Language.Binding;

using TmIde.Controls.Particles;
using TmIde.Services.Audio;
using TmIde.Services.Execution;

namespace TmIde.ViewModels.Particles;

/// <summary>
/// Tipos de eventos musicales que recibe el visualizador.
/// </summary>
public enum MusicalVisualEventKind
{
    Started,
    Released,
    Cleared
}

/// <summary>
/// Evento musical pendiente de procesarse en el orbe.
/// </summary>
public readonly record struct MusicalVisualEvent(
    MusicalVisualEventKind Kind,
    int MidiNote = 0,
    bool Sustained = false);

/// <summary>
/// Estado del visualizador de partículas.
///
/// Conserva el estado de diagnósticos y las ráfagas
/// de pulse, y añade eventos del piano.
/// </summary>
public sealed class ParticlePanelViewModel : ViewModelBase
{
    private const int MaxQueuedMusicEvents = 128;

    private readonly IdeRuntimePulse _runtimePulse;

    // Buzón independiente de eventos musicales.
    private readonly ConcurrentQueue<MusicalVisualEvent>
        _musicEvents = new();

    // Último análisis del código.
    private volatile ProjectAnalyzedMessage _latest =
        new(
            DiagnosticMood.Correct,
            TempoLimits.DefaultBpm);

    public ParticlePanelViewModel(
        IdeRuntimePulse runtimePulse,
        IMessenger messenger,
        TypingPianoService? piano = null)
    {
        _runtimePulse = runtimePulse;

        // Conservar el sistema original de diagnósticos.
        messenger.Register<
            ParticlePanelViewModel,
            ProjectAnalyzedMessage>(
            this,
            static (panel, message) =>
                panel._latest = message);

        // Escuchar las notas del servicio de piano.
        // El parámetro opcional conserva la compatibilidad
        // con las pruebas existentes del ViewModel.
        if (piano is not null)
        {
            piano.NotePlayed += (_, note) =>
            {
                QueueMusicEvent(
                    new MusicalVisualEvent(
                        MusicalVisualEventKind.Started,
                        note.MidiNote,
                        piano.IsPerformanceMode));
            };

            piano.NoteReleased += (_, note) =>
            {
                QueueMusicEvent(
                    new MusicalVisualEvent(
                        MusicalVisualEventKind.Released,
                        note.MidiNote));
            };

            piano.NotesStopped += (_, _) =>
            {
                QueueMusicEvent(
                    new MusicalVisualEvent(
                        MusicalVisualEventKind.Cleared));
            };
        }
    }

    public DiagnosticMood Mood => _latest.Mood;

    public int TempoBpm => _latest.TempoBpm;

    /// <summary>
    /// Ráfagas originales del lenguaje Tempo.
    /// </summary>
    public IReadOnlyList<double> TakeBursts() =>
        _runtimePulse.TakeBursts();

    /// <summary>
    /// Notas musicales pendientes para el siguiente
    /// fotograma del visualizador.
    /// </summary>
    public IReadOnlyList<MusicalVisualEvent>
        TakeMusicalEvents()
    {
        var events = new List<MusicalVisualEvent>();

        while (_musicEvents.TryDequeue(out var musicalEvent))
        {
            events.Add(musicalEvent);
        }

        return events;
    }

    /// <summary>
    /// Mantener acotada la cantidad de eventos.
    /// Una escritura muy rápida no debe saturar
    /// el visualizador.
    /// </summary>
    private void QueueMusicEvent(
        MusicalVisualEvent musicalEvent)
    {
        if (musicalEvent.Kind ==
            MusicalVisualEventKind.Cleared)
        {
            // El apagado tiene prioridad.
            while (_musicEvents.TryDequeue(out _))
            {
            }
        }
        else if (_musicEvents.Count >=
                 MaxQueuedMusicEvents)
        {
            _musicEvents.TryDequeue(out _);
        }

        _musicEvents.Enqueue(musicalEvent);
    }
}
