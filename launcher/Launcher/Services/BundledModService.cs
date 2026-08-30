using System.Collections.Generic;
using System.IO;

namespace Hitboxes.Launcher.Services;

/// <summary>
/// The launcher ships one ReVerfyx Client mod jar per supported Minecraft
/// version alongside its own .exe (see the CI "Build mod" matrix + "Bundle
/// mod" step, which name each build "ReVerfyxClient-&lt;mcversion&gt;.jar" in a
/// "bundled-mod" folder next to the published launcher). This installs only
/// the jar matching a given Fabric instance's own Minecraft version — never
/// a jar built for a different version, since Fabric Loader would refuse to
/// (or worse, silently misbehave on) a mod jar declaring the wrong
/// "minecraft" dependency in its fabric.mod.json.
/// </summary>
public static class BundledModService
{
    private const string BundledModDirName = "bundled-mod";

    public static readonly IReadOnlyList<string> SupportedMcVersions = new[] { "1.16.5", "1.21.4" };

    /// <returns>true if a matching build was found and installed; false if this
    /// Minecraft version isn't one ReVerfyx Client currently ships for.</returns>
    public static bool EnsureReVerfyxClientInstalled(string modsDir, string mcVersion)
    {
        string bundledDir = Path.Combine(AppContext.BaseDirectory, BundledModDirName);
        string sourcePath = Path.Combine(bundledDir, $"ReVerfyxClient-{mcVersion}.jar");
        if (!File.Exists(sourcePath))
        {
            return false; // No build for this MC version (or a local dev build with nothing bundled at all).
        }

        // Always overwrite: an instance's McVersion can change between
        // launches (e.g. after editing it), and a stale wrong-version jar
        // left under the same destination name must not linger.
        string destination = Path.Combine(modsDir, "ReVerfyxClient.jar");
        File.Copy(sourcePath, destination, overwrite: true);
        return true;
    }
}
