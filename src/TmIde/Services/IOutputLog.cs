namespace TmIde.Services;

/// <summary>
/// Mensajes para el usuario en el panel Output ("COMPILE → …", "INICIO → …").
/// No es el log de diagnóstico interno: eso va por <c>ILogger</c>.
/// </summary>

public interface IOutputLog
{
    void WriteLine(string message);
}
