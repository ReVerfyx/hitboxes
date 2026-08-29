using System.Security.Cryptography;
using System.Text;

namespace Hitboxes.Launcher.Services;

/// <summary>
/// Offline-mode "authentication": vanilla Minecraft in offline mode accepts
/// any username and derives a stable UUID from it via
/// UUID.nameUUIDFromBytes(("OfflinePlayer:" + name).getBytes(UTF-8)),
/// which is a version-3 (name-based, MD5) UUID. No network call, no
/// Microsoft account — this only works against servers/worlds running in
/// offline mode (the default for singleplayer / LAN).
/// </summary>
public static class AuthService
{
    public static Guid OfflineUuidFor(string username)
    {
        byte[] input = Encoding.UTF8.GetBytes("OfflinePlayer:" + username);
        byte[] hash = MD5.HashData(input);

        // Stamp the MD5 hash as a version-3 UUID, matching Java's
        // UUID.nameUUIDFromBytes exactly (variant/version bits set below).
        hash[6] = (byte)((hash[6] & 0x0F) | 0x30); // version 3
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80); // variant 2

        // Java's UUID(bytes) reads the two 8-byte halves as big-endian
        // longs; .NET's Guid constructor expects little-endian for the
        // first three fields, so the bytes need reordering.
        byte[] guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        Array.Reverse(guidBytes, 0, 4);
        Array.Reverse(guidBytes, 4, 2);
        Array.Reverse(guidBytes, 6, 2);

        return new Guid(guidBytes);
    }

    public static bool IsValidUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length is < 3 or > 16)
        {
            return false;
        }
        return username.All(c => char.IsLetterOrDigit(c) || c == '_');
    }
}
