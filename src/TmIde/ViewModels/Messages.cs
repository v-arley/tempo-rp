using Tempo.Language;

using TmIde.Controls.Particles;
using TmIde.ViewModels.Editor;

namespace TmIde.ViewModels;

// Mensajes entre ViewModels por el IMessenger de CommunityToolkit.Mvvm: quien
// envía no conoce a quien recibe. El messenger es WeakReferenceMessenger, así que
// registrarse no impide que un receptor se libere.

/// <summary>El explorador pide abrir el archivo de <paramref name="FilePath"/> (ruta completa).</summary>
public sealed record OpenFileRequestedMessage(string FilePath);

/// <summary>Cambió la pestaña activa del editor; <c>null</c> si no queda ninguna.</summary>
public sealed record ActiveDocumentChangedMessage(EditorDocumentViewModel? Document);

/// <summary>
/// Terminó un análisis del proyecto (al editar o con COMPILE). Puede llegar
/// desde un hilo de fondo.
/// </summary>
public sealed record ProjectAnalyzedMessage(DiagnosticMood Mood, int TempoBpm)
{
    public static ProjectAnalyzedMessage From(Compilation compilation) =>
        new(DiagnosticMood.From(compilation.Diagnostics), compilation.TempoBpm);
}