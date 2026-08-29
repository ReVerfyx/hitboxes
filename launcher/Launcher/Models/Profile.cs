namespace Hitboxes.Launcher.Models;

/// <summary>
/// A local, offline-mode identity — just a chosen username and a
/// deterministic UUID derived from it (same algorithm the vanilla client
/// uses in offline mode). No Microsoft/Mojang account is involved.
/// </summary>
public sealed class Profile
{
    public string Username { get; set; } = "Player";

    public Guid OfflineUuid => AuthService.OfflineUuidFor(Username);
}
