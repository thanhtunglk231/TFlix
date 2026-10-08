using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace DataServiceLib.Implements
{
    public class SupabaseService : ISupabaseService
    {
        private readonly RestClient? _client;
        private readonly string _supabaseUrl;
        private readonly string _storageBucket;
        private readonly ILogger<SupabaseService> _logger;

        public SupabaseService(IConfiguration configuration, ILogger<SupabaseService> logger)
        {
            _logger = logger;
            var supabase = configuration.GetSection("Supabase");
            _supabaseUrl = supabase["Url"]?.TrimEnd('/') ?? string.Empty;
            var apiKey = supabase["ApiKey"] ?? string.Empty;
            _storageBucket = supabase["Bucket"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(_supabaseUrl) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(_storageBucket))
            {
                _logger.LogWarning(
                    "Supabase storage chưa được cấu hình đầy đủ. Các API đọc dữ liệu vẫn hoạt động; upload/xóa Supabase sẽ bị vô hiệu hóa.");
                return;
            }

            var options = new RestClientOptions(_supabaseUrl);
            _client = new RestClient(options);

            // Header mặc định cho mọi request
            _client.AddDefaultHeader("apikey", apiKey);
            _client.AddDefaultHeader("Authorization", $"Bearer {apiKey}");
        }

        private static string EncodeObjectPath(string objectPath)
        {
            // Encode từng segment, giữ nguyên dấu '/'
            var segments = objectPath
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString);
            return string.Join("/", segments);
        }

        private static string NormalizeObjectPath(string pathOrUrl)
        {
            // Các prefix có thể gặp
            // public:  {url}/storage/v1/object/public/{bucket}/{object}
            // private: {url}/storage/v1/object/{bucket}/{object}
            string trimmed = pathOrUrl.Trim();

            // Nếu là public URL
            int idxPublic = trimmed.IndexOf("/storage/v1/object/public/", StringComparison.OrdinalIgnoreCase);
            if (idxPublic >= 0)
            {
                var afterPublic = trimmed.Substring(idxPublic + "/storage/v1/object/public/".Length); // "{bucket}/{object}"
                var idxSlash = afterPublic.IndexOf('/');
                if (idxSlash >= 0) return afterPublic.Substring(idxSlash + 1); // bỏ "{bucket}/"
                return string.Empty;
            }

            // Nếu là private URL
            int idxPrivate = trimmed.IndexOf("/storage/v1/object/", StringComparison.OrdinalIgnoreCase);
            if (idxPrivate >= 0)
            {
                var after = trimmed.Substring(idxPrivate + "/storage/v1/object/".Length); // "{bucket}/{object}"
                var idxSlash = after.IndexOf('/');
                if (idxSlash >= 0) return after.Substring(idxSlash + 1); // bỏ "{bucket}/"
                return string.Empty;
            }

            // Coi như truyền thẳng object path
            return trimmed.TrimStart('/');
        }

        /// <summary>
        /// Xoá 1 object trong bucket. Tham số có thể là public URL hoặc object path ("products/abc.png").
        /// </summary>
        public async Task<bool> DeleteFileAsync(string pathOrUrl)
        {
            if (_client == null)
            {
                _logger.LogWarning("Không thể xóa file vì Supabase storage chưa được cấu hình.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(pathOrUrl))
                return false;

            // Chuẩn hoá về object path bên trong bucket
            string objectPath = NormalizeObjectPath(pathOrUrl);
            if (string.IsNullOrWhiteSpace(objectPath))
                return false;

            // Encode từng segment để tránh lỗi ký tự đặc biệt
            string encodedPath = EncodeObjectPath(objectPath);

            // Dùng đường dẫn tương đối vì đã set BaseUrl ở RestClient
            var relativeUrl = $"/storage/v1/object/{_storageBucket}/{encodedPath}";

            var request = new RestRequest(relativeUrl, Method.Delete);

            var response = await _client.ExecuteAsync(request);
            if (response.IsSuccessful)
                return true;

            _logger.LogWarning(
                response.ErrorException,
                "Supabase delete failed with status {StatusCode}: {ErrorMessage}",
                (int)response.StatusCode,
                response.ErrorMessage);
            return false;
        }

        /// <summary>
        /// Upload 1 file với đường dẫn chỉ định (objectPath) và trả về public URL.
        /// Dùng cho HLS để giữ nguyên tên file trong playlist/segments.
        /// </summary>
        public async Task<string?> UploadFileAsync(IFormFile file, string objectPath)
        {
            if (_client == null)
            {
                _logger.LogWarning("Không thể upload file vì Supabase storage chưa được cấu hình.");
                return null;
            }

            if (file == null || file.Length == 0)
                return null;

            _logger.LogInformation("Uploading storage object with size {FileSize} bytes", file.Length);

            if (string.IsNullOrWhiteSpace(objectPath))
                throw new ArgumentException("objectPath không được rỗng.", nameof(objectPath));

            // Bỏ dấu / đầu nếu có
            objectPath = objectPath.TrimStart('/');

            // Encode path (từng segment)
            var encodedPath = EncodeObjectPath(objectPath);

            var relativeUrl = $"/storage/v1/object/{_storageBucket}/{encodedPath}";

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var request = new RestRequest(relativeUrl, Method.Put);
            request.AddHeader("Content-Type", file.ContentType);
            request.AddParameter(file.ContentType, memoryStream.ToArray(), ParameterType.RequestBody);

            var response = await _client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                var publicUrl =
                    $"{_supabaseUrl}/storage/v1/object/public/{_storageBucket}/{encodedPath}";
                _logger.LogInformation("Supabase upload completed with status {StatusCode}", (int)response.StatusCode);
                return publicUrl;
            }

            _logger.LogWarning(
                response.ErrorException,
                "Supabase upload failed with status {StatusCode}: {ErrorMessage}",
                (int)response.StatusCode,
                response.ErrorMessage);
            return null;
        }

        /// <summary>
        /// Upload 1 file lên bucket với đường dẫn auto (products/{guid}.ext) và trả về public URL.
        /// Giữ cho code cũ vẫn chạy được.
        /// </summary>
        public Task<string?> UploadFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Task.FromResult<string?>(null);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var objectPath = $"products/{fileName}";

            return UploadFileAsync(file, objectPath);
        }
    }
}
