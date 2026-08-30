using System.Runtime.InteropServices;

namespace Hitboxes.Launcher.Services;

/// <summary>Detects total physical RAM so the Settings and instance-override memory pickers can offer a realistic GB range.</summary>
internal static class SystemMemory
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

    public static List<int> BuildMemoryOptionsGb()
    {
        var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        ulong totalBytes = GlobalMemoryStatusEx(ref status) ? status.TotalPhys : 16UL * 1024 * 1024 * 1024;
        int maxGb = Math.Max(4, (int)(totalBytes / (1024UL * 1024 * 1024)));
        return Enumerable.Range(4, maxGb - 3).ToList();
    }
}
