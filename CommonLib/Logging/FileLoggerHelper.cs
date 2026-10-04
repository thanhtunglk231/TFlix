using System;
using Microsoft.Extensions.Logging;

namespace CommonLib.Logging
{
    /// <summary>
    /// Static Helper để ghi log lỗi trực tiếp vào file txt từ bất kỳ vị trí nào
    /// </summary>
    public static class FileLoggerHelper
    {
        private static FileLoggerOptions _defaultOptions = new FileLoggerOptions
        {
            LogDirectory = "Logs",
            FileNamePrefix = "error_log",
            MinLevel = LogLevel.Error,
            RetainDays = 30
        };

        private static FileLogger _logger = new FileLogger("StaticFileLogger", _defaultOptions);

        public static void Configure(Action<FileLoggerOptions> configure)
        {
            configure(_defaultOptions);
            _logger = new FileLogger("StaticFileLogger", _defaultOptions);
        }

        public static void LogError(Exception ex, string message = "")
        {
            _logger.LogError(ex, message);
        }

        public static void LogError(string message)
        {
            _logger.LogError(message);
        }

        public static void LogWarning(string message)
        {
            _logger.LogWarning(message);
        }

        public static void LogInformation(string message)
        {
            _logger.LogInformation(message);
        }
    }
}
