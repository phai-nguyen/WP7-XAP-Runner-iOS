using System.Text;

namespace WP7ILRun1;

internal static class AppLog
{
    private static readonly object Sync = new();
    private static bool _initialized;
    private static string? _logDirectory;
    private static string? _takeThisPath;
    private static string? _persistentPath;

    public static string? LogDirectory => _logDirectory;
    public static string? TakeThisPath => _takeThisPath;
    public static string? PersistentPath => _persistentPath;

    public static void Initialize()
    {
        lock (Sync)
        {
            if (_initialized)
                return;

            _initialized = true;

            try
            {
                var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (string.IsNullOrWhiteSpace(documents))
                    return;

                _logDirectory = Path.Combine(documents, "WP7RunnerLogs");
                Directory.CreateDirectory(_logDirectory);

                _takeThisPath = Path.Combine(_logDirectory, "WP7Runner_TakeThis.log");
                _persistentPath = Path.Combine(_logDirectory, "WP7Runner_Persistent.log");
                var previousPath = Path.Combine(_logDirectory, "WP7Runner_Persistent-prev.log");

                if (File.Exists(_persistentPath))
                {
                    try
                    {
                        File.Copy(_persistentPath, previousPath, overwrite: true);
                    }
                    catch
                    {
                        // Logging must never stop application startup.
                    }
                }

                File.WriteAllText(_takeThisPath, string.Empty, Encoding.UTF8);
                AppendRaw(_persistentPath, Environment.NewLine + "===== NEW PROCESS =====" + Environment.NewLine);
            }
            catch
            {
                // Never throw from the logger.
            }
        }
    }

    public static void Write(string message)
    {
        lock (Sync)
        {
            Initialize();

            var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} {message}";
            try { Console.WriteLine(line); } catch { }

            if (_takeThisPath is not null)
                AppendRaw(_takeThisPath, line + Environment.NewLine);

            if (_persistentPath is not null)
                AppendRaw(_persistentPath, line + Environment.NewLine);
        }
    }

    private static void AppendRaw(string path, string text)
    {
        try
        {
            File.AppendAllText(path, text, Encoding.UTF8);
        }
        catch
        {
            // Deliberately swallow: logging must not become a new failure source.
        }
    }
}
