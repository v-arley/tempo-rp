using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

namespace TmIde.Services.Settings;

/// <summary>
/// <see cref="IUserSettingsStore"/> sobre un archivo JSON. Perder las
/// preferencias no debe impedir usar el IDE: un archivo dañado o imposible de
/// escribir se registra y se sigue con los valores por defecto.
/// </summary>

public sealed partial class JsonUserSettingsStore : IUserSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _filePath;
    private readonly ILogger<JsonUserSettingsStore> _logger;

    public JsonUserSettingsStore(string filePath, ILogger<JsonUserSettingsStore> logger)
    {
        _filePath = filePath;
        _logger = logger;
    }

    /// <summary>Ubicación por defecto: <c>%LOCALAPPDATA%\TmIde\settings.json</c>.</summary>
    public static string DefaultFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TmIde",
        "settings.json");

    public UserSettings Load()
    {
        if (!File.Exists(_filePath))
        {
            return new UserSettings();
        }

        try
        {
            return JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(_filePath), JsonOptions) ?? new UserSettings();
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            LogLoadFailed(exception, _filePath);
            return new UserSettings();
        }
    }

    public void Save(UserSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

            // Se escribe aparte y se reemplaza: un cierre a mitad de escritura no
            // deja el archivo a medias.
            var temporary = _filePath + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(settings, JsonOptions));
            File.Move(temporary, _filePath, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            LogSaveFailed(exception, _filePath);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudieron leer las preferencias de {FilePath}; se usan las de por defecto")]
    private partial void LogLoadFailed(Exception exception, string filePath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudieron guardar las preferencias en {FilePath}")]
    private partial void LogSaveFailed(Exception exception, string filePath);
}
