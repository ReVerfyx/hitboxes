using System.Diagnostics;
using System.IO;

namespace Hitboxes.Launcher.Services;

/// <summary>
/// Captures a launched game process's stdout/stderr to a per-instance log
/// file — javaw has no console window at all, so without this, a crash
/// before Minecraft's own logging even starts (bad natives, wrong Java
/// version, a classpath problem) is completely invisible: no window, no
/// error, nothing happens as far as the user can tell. Also watches for an
/// early exit (well before a real play session would end) so the UI can
/// say something more useful than a silent "Запущено".
/// </summary>
public static class LaunchLogService
{
    private static readonly TimeSpan EarlyExitThreshold = TimeSpan.FromSeconds(20);
    private const int MaxCapturedLinesForCrashReport = 60;

    /// <summary>Raised off the UI thread when the process exits within
    /// EarlyExitThreshold of starting with a non-zero exit code — a strong
    /// signal it crashed rather than the user closing the game normally.
    /// Argument is the path to that launch's full captured log.</summary>
    public static event Action<string>? EarlyCrashDetected;

    public static void Attach(Process process, string rootDir, string instanceId)
    {
        string logsDir = Path.Combine(rootDir, "logs");
        Directory.CreateDirectory(logsDir);
        string logPath = Path.Combine(logsDir, $"game-{instanceId}.log");

        var writer = new StreamWriter(logPath, append: false) { AutoFlush = true };
        var recentLines = new Queue<string>();
        var sync = new object();
        DateTime startedAt = DateTime.UtcNow;

        void OnLine(object? sender, DataReceivedEventArgs e)
        {
            if (e.Data is null)
            {
                return;
            }

            lock (sync)
            {
                try
                {
                    writer.WriteLine(e.Data);
                }
                catch
                {
                    // Best-effort logging must never take the game process down with it.
                }

                recentLines.Enqueue(e.Data);
                if (recentLines.Count > MaxCapturedLinesForCrashReport)
                {
                    recentLines.Dequeue();
                }
            }
        }

        process.OutputDataReceived += OnLine;
        process.ErrorDataReceived += OnLine;
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.EnableRaisingEvents = true;
        process.Exited += (_, _) =>
        {
            lock (sync)
            {
                try
                {
                    writer.Flush();
                    writer.Dispose();
                }
                catch
                {
                    // Best-effort.
                }
            }

            int exitCode = SafeExitCode(process);
            DevLog.Log($"Игровой процесс завершён (код {exitCode}). Полный лог: {logPath}");

            if (exitCode != 0 && DateTime.UtcNow - startedAt < EarlyExitThreshold)
            {
                string excerpt;
                lock (sync)
                {
                    excerpt = string.Join(Environment.NewLine, recentLines);
                }
                DevLog.Log($"РАННИЙ ВЫХОД (похоже на краш, код {exitCode}) — последние строки вывода:\n{excerpt}");
                EarlyCrashDetected?.Invoke(logPath);
            }
        };
    }

    private static int SafeExitCode(Process process)
    {
        try
        {
            return process.ExitCode;
        }
        catch
        {
            return -1;
        }
    }
}
