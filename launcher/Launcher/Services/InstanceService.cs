using System.IO;
using System.Text.Json;
using Hitboxes.Launcher.Models;

namespace Hitboxes.Launcher.Services;

/// <summary>
/// CRUD over `instances/&lt;id&gt;/instance.json`. Each instance folder also
/// holds that instance's own `minecraft/` game directory and `mods/`
/// folder, so instances never share state the way a single shared profile
/// would.
/// </summary>
public sealed class InstanceService
{
    private readonly string _instancesDir;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public InstanceService(string rootDir)
    {
        _instancesDir = Path.Combine(rootDir, "instances");
        Directory.CreateDirectory(_instancesDir);
    }

    public string GetInstanceDir(Instance instance) => Path.Combine(_instancesDir, instance.Id);

    public string GetGameDir(Instance instance) => Path.Combine(GetInstanceDir(instance), "minecraft");

    public string GetModsDir(Instance instance)
    {
        string dir = Path.Combine(GetGameDir(instance), "mods");
        Directory.CreateDirectory(dir);
        return dir;
    }

    public List<Instance> LoadAll()
    {
        var result = new List<Instance>();
        foreach (var dir in Directory.EnumerateDirectories(_instancesDir))
        {
            string path = Path.Combine(dir, "instance.json");
            if (!File.Exists(path))
            {
                continue;
            }
            try
            {
                var instance = JsonSerializer.Deserialize<Instance>(File.ReadAllText(path));
                if (instance is not null)
                {
                    result.Add(instance);
                }
            }
            catch (JsonException)
            {
                // Skip a corrupted instance.json rather than crashing the list.
            }
        }
        return result.OrderByDescending(i => i.LastPlayedAt ?? i.CreatedAt).ToList();
    }

    public void Save(Instance instance)
    {
        string dir = GetInstanceDir(instance);
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "instance.json");
        File.WriteAllText(path, JsonSerializer.Serialize(instance, JsonOptions));
    }

    public void Delete(Instance instance)
    {
        string dir = GetInstanceDir(instance);
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>Copies an instance's entire folder (game dir, mods, everything) under a new Id, Prism-style "Копировать".</summary>
    public Instance Duplicate(Instance source)
    {
        var copy = new Instance
        {
            Name = source.Name + " (копия)",
            McVersion = source.McVersion,
            Loader = source.Loader,
            FabricLoaderVersion = source.FabricLoaderVersion,
            IconKey = source.IconKey,
            MemoryMinMb = source.MemoryMinMb,
            MemoryMaxMb = source.MemoryMaxMb,
            ExtraJvmArgs = source.ExtraJvmArgs,
            JavaExecutableOverride = source.JavaExecutableOverride,
        };

        CopyDirectory(GetInstanceDir(source), GetInstanceDir(copy));
        Save(copy);
        return copy;
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (string file in Directory.EnumerateFiles(sourceDir))
        {
            File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)));
        }
        foreach (string dir in Directory.EnumerateDirectories(sourceDir))
        {
            CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
        }
    }
}
