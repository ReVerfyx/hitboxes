using System.IO;

namespace Hitboxes.Launcher.Services;

/// <summary>
/// The launcher ships the ReVerfyx Client mod jar alongside its own .exe
/// (see the CI "Bundle ReVerfyx Client mod into launcher output" step,
/// which copies the mod build's jar into a "bundled-mod" folder next to
/// the published launcher). This drops that jar into a Fabric instance's
/// mods folder automatically — no manual Modrinth search needed for the
/// launcher's own client mod.
/// </summary>
public static class BundledModService
{
    private const string BundledModDirName = "bundled-mod";

    public static void EnsureReVerfyxClientInstalled(string modsDir)
    {
        string bundledDir = Path.Combine(AppContext.BaseDirectory, BundledModDirName);
        if (!Directory.Exists(bundledDir))
        {
            return; // Not present in a local dev build — nothing to bundle.
        }

        foreach (string jarPath in Directory.EnumerateFiles(bundledDir, "*.jar"))
        {
            string destination = Path.Combine(modsDir, Path.GetFileName(jarPath));
            if (!File.Exists(destination))
            {
                File.Copy(jarPath, destination);
            }
        }
    }
}
