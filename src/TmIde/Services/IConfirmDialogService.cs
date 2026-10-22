namespace TmIde.Services;

/// <summary>
/// Pregunta sí/no al usuario. Abstrae el cuadro de diálogo para que los
/// ViewModels no dependan de tipos de vista.
/// </summary>

public interface IConfirmDialogService
{
    /// <returns><c>true</c> si el usuario acepta.</returns>
    bool Confirm(string message);
}
