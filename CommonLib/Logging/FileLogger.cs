using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace CommonLib.Logging
{
    public class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly FileLoggerOptions _options;
        private static readonly object _lock = new object();

        public FileLogger(string categoryName, FileLoggerOptions options)
        {
            _categoryName = categoryName;
            _options = options ?? new FileLoggerOptions();
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _options.MinLevel;
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

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            string message = formatter(state, exception);
            if (string.IsNullOrEmpty(message) && exception == null)
            {
                return;
            }

            DateTime now = DateTime.Now;
            string logMessage = FormatLogEntry(now, logLevel, message, exception);

            WriteToFile(now, logMessage);
        }

        private string FormatLogEntry(DateTime now, LogLevel logLevel, string message, Exception? exception)
        {
            var sb = new StringBuilder();
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine($"[{now:yyyy-MM-dd HH:mm:ss.fff}] [{logLevel.ToString().ToUpper()}] [{_categoryName}]");
            
            if (!string.IsNullOrWhiteSpace(message))
            {
                sb.AppendLine($"Message: {message}");
            }

            if (exception != null)
            {
                sb.AppendLine($"Exception Type: {exception.GetType().FullName}");
                sb.AppendLine($"Exception Msg : {exception.Message}");
                sb.AppendLine($"StackTrace    :\n{exception.StackTrace}");

                if (exception.InnerException != null)
                {
                    sb.AppendLine($"Inner Exception: {exception.InnerException.Message}");
                    sb.AppendLine($"Inner StackTrace:\n{exception.InnerException.StackTrace}");
                }
            }
            
            return sb.ToString();
        }

        private void WriteToFile(DateTime now, string logEntry)
        {
            lock (_lock)
            {
                try
                {
                    string directory = Path.IsPathRooted(_options.LogDirectory)
                        ? _options.LogDirectory
                        : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _options.LogDirectory);

                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    string fileName = $"{_options.FileNamePrefix}_{now:yyyy-MM-dd}.txt";
                    string filePath = Path.Combine(directory, fileName);

                    File.AppendAllText(filePath, logEntry, Encoding.UTF8);

                    CleanOldLogs(directory);
                }
                catch
                {
                    // Tránh crash ứng dụng do lỗi ghi log file (ví dụ permission)
                }
            }
        }

        private void CleanOldLogs(string directory)
        {
            if (_options.RetainDays <= 0) return;

            try
            {
                var dirInfo = new DirectoryInfo(directory);
                DateTime threshold = DateTime.Now.AddDays(-_options.RetainDays);

                foreach (var file in dirInfo.GetFiles($"{_options.FileNamePrefix}_*.txt"))
                {
                    if (file.CreationTime < threshold && file.LastWriteTime < threshold)
                    {
                        file.Delete();
                    }
                }
            }
            catch
            {
                // Bỏ qua lỗi xóa file cũ nếu đang bị khóa
            }
        }
    }
}
