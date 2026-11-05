using System.Collections.Generic;

using CommunityToolkit.Mvvm.Messaging;

using Tempo.Language.Binding;

using TmIde.Controls.Particles;
using TmIde.Services.Execution;

namespace TmIde.ViewModels.Particles;

/// <summary>
/// Estado del panel de partículas: el ánimo del proyecto y su tempo.
/// Escucha el análisis del proyecto; la animación en sí vive en el control
/// <c>ParticlePanel</c>, que lee estos valores en cada fotograma.
/// </summary>

public sealed class ParticlePanelViewModel : ViewModelBase
{
    private readonly IdeRuntimePulse _runtimePulse;

    // Último análisis recibido. El análisis en vivo avisa desde un hilo de fondo
    // y la animación lee en el de la UI: reemplazar una referencia es atómico, así
    // que ánimo y tempo siempre se leen juntos y del mismo análisis.
    private volatile ProjectAnalyzedMessage _latest = new(DiagnosticMood.Correct, TempoLimits.DefaultBpm);

    public ParticlePanelViewModel(IdeRuntimePulse runtimePulse, IMessenger messenger)
    {
        _runtimePulse = runtimePulse;

        messenger.Register<ParticlePanelViewModel, ProjectAnalyzedMessage>(
            this, static (panel, message) => panel._latest = message);
    }

    public DiagnosticMood Mood => _latest.Mood;

    public int TempoBpm => _latest.TempoBpm;

    /// <summary>Ráfagas de <c>pulse</c> llegadas desde el fotograma anterior.</summary>
    public IReadOnlyList<double> TakeBursts() => _runtimePulse.TakeBursts();
}
