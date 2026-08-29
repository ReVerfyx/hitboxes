using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using Hitboxes.Launcher.Models;

namespace Hitboxes.Launcher.Services;

/// <summary>
/// Builds the same JVM/game argument line a real Mojang launcher would and
/// starts the local `java` executable. Offline mode: the access token is a
/// dummy value and user_type is "legacy", which is what vanilla accepts
/// for an unauthenticated session in offline play.
/// </summary>
public sealed class GameLauncher
{
    private readonly string _rootDir;

    public GameLauncher(string rootDir)
    {
        _rootDir = rootDir;
    }

    public Process Launch(InstalledVersion installed, Profile profile, Instance instance,
        LauncherSettings settings, string gameDir)
    {
        Directory.CreateDirectory(gameDir);

        string classpath = string.Join(Path.PathSeparator, installed.ClasspathEntries);

        var substitutions = new Dictionary<string, string>
        {
            ["auth_player_name"] = profile.Username,
            ["version_name"] = installed.Detail.Id,
            ["game_directory"] = gameDir,
            ["assets_root"] = Path.Combine(_rootDir, "assets"),
            ["assets_index_name"] = installed.Detail.AssetsIndexId,
            ["auth_uuid"] = profile.OfflineUuid.ToString("N"),
            ["auth_access_token"] = "0",
            ["user_type"] = "legacy",
            ["version_type"] = "hitboxes-launcher",
            ["natives_directory"] = installed.NativesDir,
            ["launcher_name"] = "hitboxes-launcher",
            ["launcher_version"] = "0.1.0",
            ["classpath"] = classpath,
        };

        int memMin = instance.MemoryMinMb ?? settings.DefaultMemoryMinMb;
        int memMax = instance.MemoryMaxMb ?? settings.DefaultMemoryMaxMb;
        string extraJvmArgs = instance.ExtraJvmArgs ?? settings.DefaultJvmArgs;
        string javaExecutable = instance.JavaExecutableOverride ?? settings.JavaExecutable;

        var args = new List<string>();
        args.AddRange(BuildJvmArgs(installed.Detail, substitutions, memMin, memMax, extraJvmArgs));
        args.Add(installed.Detail.MainClass);
        args.AddRange(BuildGameArgs(installed.Detail, substitutions));

        var psi = new ProcessStartInfo
        {
            FileName = javaExecutable,
            WorkingDirectory = gameDir,
            UseShellExecute = false,
        };
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        return Process.Start(psi) ?? throw new InvalidOperationException("Failed to start Java process.");
    }

    private static IEnumerable<string> BuildJvmArgs(VersionDetail detail, Dictionary<string, string> subs,
        int memMinMb, int memMaxMb, string extraJvmArgs)
    {
        yield return $"-Djava.library.path={subs["natives_directory"]}";
        yield return $"-Xms{memMinMb}M";
        yield return $"-Xmx{memMaxMb}M";
        yield return "-cp";
        yield return subs["classpath"];

        foreach (var extra in extraJvmArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            yield return extra;
        }

        if (detail.Arguments is null)
        {
            yield break;
        }

        foreach (var element in detail.Arguments.Jvm)
        {
            foreach (var value in ResolveArgumentElement(element))
            {
                yield return Substitute(value, subs);
            }
        }
    }

    private static IEnumerable<string> BuildGameArgs(VersionDetail detail, Dictionary<string, string> subs)
    {
        if (detail.Arguments is null)
        {
            yield break;
        }

        foreach (var element in detail.Arguments.Game)
        {
            foreach (var value in ResolveArgumentElement(element))
            {
                yield return Substitute(value, subs);
            }
        }
    }

    /// <summary>
    /// Each "arguments" entry is either a plain string or a conditional
    /// object ({"rules": [...], "value": "..."}). We keep this simple:
    /// only unconditional string entries are emitted, since 1.16.5+
    /// singleplayer/offline launches don't need the OS/feature-gated
    /// extras (demo mode, Twitch, etc.) those conditionals guard.
    /// </summary>
    private static IEnumerable<string> ResolveArgumentElement(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            yield return element.GetString()!;
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    yield return item.GetString()!;
                }
            }
        }
        // Object-with-"rules" entries are skipped deliberately (see summary above).
    }

    private static string Substitute(string template, Dictionary<string, string> subs)
    {
        var sb = new StringBuilder(template);
        foreach (var (key, value) in subs)
        {
            sb.Replace("${" + key + "}", value);
        }
        return sb.ToString();
    }
}
