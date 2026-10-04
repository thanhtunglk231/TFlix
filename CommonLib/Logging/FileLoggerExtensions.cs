using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CommonLib.Logging
{
    public static class FileLoggerExtensions
    {
        /// <summary>
        /// Thêm FileLogger vào ứng dụng ASP.NET Core
        /// </summary>
        public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, Action<FileLoggerOptions>? configure = null)
        {
            var options = new FileLoggerOptions();
            configure?.Invoke(options);

            builder.Services.AddSingleton<ILoggerProvider>(new FileLoggerProvider(options));
            return builder;
        }

        /// <summary>
        /// Thêm FileLogger từ ILoggerFactory
        /// </summary>
        public static ILoggerFactory AddFileLogger(this ILoggerFactory factory, Action<FileLoggerOptions>? configure = null)
        {
            var options = new FileLoggerOptions();
            configure?.Invoke(options);

            factory.AddProvider(new FileLoggerProvider(options));
            return factory;
        }
    }
}
