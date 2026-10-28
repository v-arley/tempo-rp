
using ICSharpCode.AvalonEdit.Document;

namespace TmIde.ViewModels.Editor;

/// <summary>
/// Solicita mover el cursor del editor a una posicion
/// determinada dentro de un documento.
/// </summary>
public sealed record EditorNavigationRequest(
    TextDocument Document,
    int Offset);
