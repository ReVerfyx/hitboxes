using System.IO;
using System.Text.Json;
using Hitboxes.Launcher.Models;

namespace Hitboxes.Launcher.Services;

public sealed class SettingsService
{
    private readonly string _settingsPath;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public SettingsService(string rootDir)
    {
        _settingsPath = Path.Combine(rootDir, "settings.json");
    }

    public LauncherSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            var defaults = new LauncherSettings();
            Save(defaults);
            return defaults;
        }

        string json = File.ReadAllText(_settingsPath);
        return JsonSerializer.Deserialize<LauncherSettings>(json) ?? new LauncherSettings();
    }

    public void Save(LauncherSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        string json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }
}
