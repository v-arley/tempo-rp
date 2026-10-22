namespace TmIde.Services.Settings;

/// <summary>Lee y guarda <see cref="UserSettings"/>.</summary>

public interface IUserSettingsStore
{
    /// <summary>Preferencias guardadas, o las de por defecto si no hay o no se pueden leer.</summary>
    UserSettings Load();

    /// <summary>Guarda las preferencias. Un fallo se registra pero no se propaga.</summary>
    void Save(UserSettings settings);
}
