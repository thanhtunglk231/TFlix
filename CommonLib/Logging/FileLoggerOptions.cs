using Microsoft.Extensions.Logging;

namespace CommonLib.Logging
{
    public class FileLoggerOptions
    {
        /// <summary>
        /// Thư mục chứa file log (mặc định: "Logs")
        /// </summary>
        public string LogDirectory { get; set; } = "Logs";

        /// <summary>
        /// Tiền tố tên file log (mặc định: "error_log")
        /// </summary>
        public string FileNamePrefix { get; set; } = "error_log";

        /// <summary>
        /// Cấp độ log tối thiểu để ghi vào file (mặc định: LogLevel.Error)
        /// </summary>
        public LogLevel MinLevel { get; set; } = LogLevel.Error;

        /// <summary>
        /// Số ngày lưu giữ file log trước khi tự động xóa file cũ (0 = không xóa)
        /// </summary>
        public int RetainDays { get; set; } = 30;
    }
}
