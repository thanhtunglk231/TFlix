using System.Collections.Concurrent;

namespace Server.Logging;

public sealed class ServerDailyFileLoggerProvider : ILoggerProvider
{
    private readonly string _logDirectory;
    private readonly ConcurrentDictionary<string, DailyFileLogger> _loggers = new();

    public ServerDailyFileLoggerProvider(string projectName)
    {
        _logDirectory = ResolveLogDirectory(projectName);
        Directory.CreateDirectory(_logDirectory);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new DailyFileLogger(name, _logDirectory));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }

    private static string ResolveLogDirectory(string projectName)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current != null)
        {
            var projectFile = Path.Combine(current.FullName, $"{projectName}.csproj");
            if (File.Exists(projectFile))
            {
                var logDirectory = Path.Combine(current.FullName, "logs");
                Directory.CreateDirectory(logDirectory);
                return logDirectory;
            }

            var projectFolder = Path.Combine(current.FullName, projectName);
            if (Directory.Exists(projectFolder))
            {
                var logDirectory = Path.Combine(projectFolder, "logs");
                Directory.CreateDirectory(logDirectory);
                return logDirectory;
            }

            current = current.Parent;
        }

        var fallback = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(fallback);
        return fallback;
    }
}

public sealed class DailyFileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly string _logDirectory;
    private readonly object _syncRoot = new();

    public DailyFileLogger(string categoryName, string logDirectory)
    {
        _categoryName = categoryName;
        _logDirectory = logDirectory;
        Directory.CreateDirectory(_logDirectory);
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return NullScope.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= LogLevel.Error;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        var logFilePath = Path.Combine(_logDirectory, $"error_{DateTime.Now:yyyyMMdd}.txt");

        var content = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] [{_categoryName}] {message}{Environment.NewLine}";

        if (exception != null)
        {
            content += $"Exception: {exception}{Environment.NewLine}";
        }

        content += "------------------------------------------------------------" + Environment.NewLine;

        lock (_syncRoot)
        {
            File.AppendAllText(logFilePath, content);
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
