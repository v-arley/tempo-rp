namespace TmIde.Services;

/// <summary>
/// Muestra la ventana modal de inicio (nuevo proyecto, abrir proyecto,
/// proyectos recientes). Abstrae la creación de la ventana para que los
/// ViewModels no dependan de tipos de vista.
/// </summary>

public interface IStartDialogService
{
    void Show();
}
