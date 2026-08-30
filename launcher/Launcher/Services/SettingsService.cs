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
        LauncherSettings settings;
        if (!File.Exists(_settingsPath))
        {
            settings = new LauncherSettings();
        }
        else
        {
            string json = File.ReadAllText(_settingsPath);
            settings = JsonSerializer.Deserialize<LauncherSettings>(json) ?? new LauncherSettings();
        }

        // Guarantee at least one account and a valid CurrentAccountId — also
        // migrates pre-multi-account settings.json files (Accounts empty)
        // to a single default "Player" account.
        if (settings.Accounts.Count == 0)
        {
            var account = new Account();
            settings.Accounts.Add(account);
            settings.CurrentAccountId = account.Id;
            Save(settings);
        }
        else if (settings.CurrentAccountId is null || settings.Accounts.All(a => a.Id != settings.CurrentAccountId))
        {
            settings.CurrentAccountId = settings.Accounts[0].Id;
            Save(settings);
        }

        return settings;
    }

    public void Save(LauncherSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        string json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }
}
