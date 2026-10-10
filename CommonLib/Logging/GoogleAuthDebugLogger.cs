using System;
using System.IO;
using System.Text;

namespace CommonLib.Logging
{
    /// <summary>
    /// Ghi log chi tiết tiến trình xác thực Google ra file Logs/google_auth_debug.txt để dễ dàng kiểm tra lỗi
    /// </summary>
    public static class GoogleAuthDebugLogger
    {
        private static readonly object _lock = new object();
        private static string _logDirectory = "Logs";

        public static void SetLogDirectory(string directory)
        {
            if (!string.IsNullOrWhiteSpace(directory))
            {
                _logDirectory = directory;
            }
        }

        public static string Mask(string? value, int keepStart = 8, int keepEnd = 6)
        {
            if (string.IsNullOrWhiteSpace(value)) return "[TRỐNG / NULL]";
            if (value.Length <= (keepStart + keepEnd)) return "***";
            return $"{value.Substring(0, keepStart)}...{value.Substring(value.Length - keepEnd)} (Độ dài: {value.Length})";
        }

        public static string MaskSecret(string? secret)
        {
            if (string.IsNullOrWhiteSpace(secret)) return "[TRỐNG / NULL]";
            if (secret.Length <= 6) return "***";
            return $"{secret.Substring(0, 3)}***{secret.Substring(secret.Length - 3)} (Độ dài: {secret.Length})";
        }

        public static void Log(string step, string message, Exception? ex = null)
        {
            try
            {
                lock (_lock)
                {
                    string dir = Path.IsPathRooted(_logDirectory)
                        ? _logDirectory
                        : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _logDirectory);

                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    string filePath = Path.Combine(dir, "google_auth_debug.txt");
                    var sb = new StringBuilder();
                    sb.AppendLine("================================================================================");
                    sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [STEP: {step}]");
                    sb.AppendLine($"Detail: {message}");
                    if (ex != null)
                    {
                        sb.AppendLine($"Exception Type: {ex.GetType().FullName}");
                        sb.AppendLine($"Exception Msg : {ex.Message}");
                        sb.AppendLine($"StackTrace    :\n{ex.StackTrace}");
                        if (ex.InnerException != null)
                        {
                            sb.AppendLine($"Inner Exception: {ex.InnerException.Message}");
                            sb.AppendLine($"Inner StackTrace:\n{ex.InnerException.StackTrace}");
                        }
                    }
                    sb.AppendLine("================================================================================");
                    sb.AppendLine();

                    File.AppendAllText(filePath, sb.ToString(), Encoding.UTF8);
                }
            }
            catch
            {
                // Tránh throw exception làm gián đoạn flow
            }
        }
    }
}
