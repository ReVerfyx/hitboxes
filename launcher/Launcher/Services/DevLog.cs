using System.IO;

namespace Hitboxes.Launcher.Services;

/// <summary>
/// In-memory + on-disk log of status messages and exceptions. Exists so a
/// user hitting a real-world failure (network timeout, missing Java, an
/// unhandled crash) can copy the exact detail from Settings → Разработчик
/// instead of retyping/screenshotting whatever the one-line StatusText
/// showed — StatusText.Text is deliberately short/user-friendly, this
/// keeps the full text (including stack traces) alongside it.
/// </summary>
public static class DevLog
{
    private const int MaxLines = 2000;
    private static readonly List<string> _lines = new();
    private static readonly object _lock = new();
    private static string? _logFilePath;

    public static event Action? Updated;

    public static void Initialize(string rootDir)
    {
        string logsDir = Path.Combine(rootDir, "logs");
        Directory.CreateDirectory(logsDir);
        _logFilePath = Path.Combine(logsDir, "launcher.log");
    }

    public static void Log(string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        lock (_lock)
        {
            _lines.Add(line);
            if (_lines.Count > MaxLines)
            {
                _lines.RemoveAt(0);
            }
        }

        // File I/O failing here (disk full, permissions) must never take
        // the app down just because logging itself couldn't complete.
        try
        {
            if (_logFilePath is not null)
            {
                File.AppendAllText(_logFilePath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Best-effort only.
        }

        Updated?.Invoke();
    }

    public static string GetAll()
    {
        lock (_lock)
        {
            return string.Join(Environment.NewLine, _lines);
        }
    }

    public static string? LogFilePath => _logFilePath;
}
