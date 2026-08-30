namespace Hitboxes.Launcher.Models;

/// <summary>One saved offline profile. The launcher supports creating and switching between
/// as many of these as the player wants — there is no single fixed "the username".</summary>
public sealed class Account
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Username { get; set; } = "Player";
}
