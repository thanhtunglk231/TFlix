using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace CommonLib.Logging
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly FileLoggerOptions _options;
        private readonly ConcurrentDictionary<string, FileLogger> _loggers = new ConcurrentDictionary<string, FileLogger>();

        public FileLoggerProvider(FileLoggerOptions options)
        {
            _options = options ?? new FileLoggerOptions();
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _options));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}
