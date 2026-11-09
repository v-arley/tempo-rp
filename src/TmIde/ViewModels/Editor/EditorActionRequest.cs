namespace TmIde.ViewModels.Editor;

/// <summary>Acciones de edición que el editor ejecuta sobre el documento visible.</summary>
public enum EditorAction
{
    /// <summary>Abre la búsqueda (Ctrl+F).</summary>
    Find,

    /// <summary>Abre la búsqueda con la fila de reemplazo (Ctrl+H).</summary>
    Replace,

    /// <summary>Muestra la lista de autocompletado (Ctrl+Espacio).</summary>
    Complete,
}

/// <summary>
/// Pide al editor que ejecute una acción, igual que <see cref="EditorNavigationRequest"/>
/// pide mover el cursor. Es una clase y no un record a propósito: la igualdad
/// por referencia hace que pedir dos veces la misma acción sean dos cambios
/// distintos de la propiedad, y el editor atienda ambos.
/// </summary>
public sealed class EditorActionRequest(EditorAction action)
{
    public EditorAction Action { get; } = action;
}
