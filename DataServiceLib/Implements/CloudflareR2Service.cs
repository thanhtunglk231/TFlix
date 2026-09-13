using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DataServiceLib.Implements
{
    public class CloudflareR2Service : ICloudflareR2Service
    {
        private const string Region = "auto";
        private const string Service = "s3";

        private readonly HttpClient _client;
        private readonly string _accountId;
        private readonly string _accessKey;
        private readonly string _secretKey;
        private readonly string _bucketName;
        private readonly string _publicBaseUrl;
        private readonly string _storageHost;

        public CloudflareR2Service(IConfiguration configuration)
        {
            _accountId = configuration["CloudflareR2:AccountId"] ?? string.Empty;
            _accessKey = configuration["CloudflareR2:AccessKey"] ?? string.Empty;
            _secretKey = configuration["CloudflareR2:SecretKey"] ?? string.Empty;
            _bucketName = configuration["CloudflareR2:BucketName"] ?? string.Empty;
            _publicBaseUrl = (configuration["CloudflareR2:PublicBaseUrl"] ?? string.Empty).TrimEnd('/');

            if (string.IsNullOrWhiteSpace(_accountId))
                throw new InvalidOperationException("CloudflareR2:AccountId chưa được cấu hình.");
            if (string.IsNullOrWhiteSpace(_accessKey))
                throw new InvalidOperationException("CloudflareR2:AccessKey chưa được cấu hình.");
            if (string.IsNullOrWhiteSpace(_secretKey))
                throw new InvalidOperationException("CloudflareR2:SecretKey chưa được cấu hình.");
            if (string.IsNullOrWhiteSpace(_bucketName))
                throw new InvalidOperationException("CloudflareR2:BucketName chưa được cấu hình.");
            if (string.IsNullOrWhiteSpace(_publicBaseUrl))
                throw new InvalidOperationException("CloudflareR2:PublicBaseUrl chưa được cấu hình.");

            _storageHost = $"{_accountId}.r2.cloudflarestorage.com";
            _client = new HttpClient
            {
                BaseAddress = new Uri($"https://{_storageHost}")
            };
        }

        public async Task<string?> UploadFileAsync(IFormFile file, string objectPath)
        {
            if (file == null || file.Length == 0)
                return null;

            if (string.IsNullOrWhiteSpace(objectPath))
                throw new ArgumentException("objectPath không được rỗng.", nameof(objectPath));

            objectPath = NormalizeObjectPath(objectPath);
            var encodedPath = EncodeObjectPath(objectPath);
            var requestPath = $"/{_bucketName}/{encodedPath}";

            await using var stream = file.OpenReadStream();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            var contentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? GetContentType(file.FileName)
                : file.ContentType;

            using var request = new HttpRequestMessage(HttpMethod.Put, requestPath)
            {
                Content = new ByteArrayContent(bytes)
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            SignRequest(request, requestPath, bytes);

            var response = await _client.SendAsync(request);
            if (response.IsSuccessStatusCode)
                return $"{_publicBaseUrl}/{encodedPath}";

            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Cloudflare R2 Upload Error: {(int)response.StatusCode} - {error}");
            return null;
        }

        public async Task<bool> DeleteFileAsync(string pathOrUrl)
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl))
                return false;

            var objectPath = NormalizeObjectPath(pathOrUrl);
            if (string.IsNullOrWhiteSpace(objectPath))
                return false;

            var encodedPath = EncodeObjectPath(objectPath);
            var requestPath = $"/{_bucketName}/{encodedPath}";

            using var request = new HttpRequestMessage(HttpMethod.Delete, requestPath);
            SignRequest(request, requestPath, Array.Empty<byte>());

            var response = await _client.SendAsync(request);
            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return true;

            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Cloudflare R2 Delete Error: {(int)response.StatusCode} - {error}");
            return false;
        }

        private void SignRequest(HttpRequestMessage request, string canonicalUri, byte[] payload)
        {
            var now = DateTime.UtcNow;
            var amzDate = now.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
            var dateStamp = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            var payloadHash = ToHex(SHA256.HashData(payload));

            request.Headers.Host = _storageHost;
            request.Headers.TryAddWithoutValidation("x-amz-content-sha256", payloadHash);
            request.Headers.TryAddWithoutValidation("x-amz-date", amzDate);

            var canonicalHeaders =
                $"host:{_storageHost}\n" +
                $"x-amz-content-sha256:{payloadHash}\n" +
                $"x-amz-date:{amzDate}\n";
            const string signedHeaders = "host;x-amz-content-sha256;x-amz-date";

            var canonicalRequest = string.Join('\n',
                request.Method.Method,
                canonicalUri,
                string.Empty,
                canonicalHeaders,
                signedHeaders,
                payloadHash);

            var credentialScope = $"{dateStamp}/{Region}/{Service}/aws4_request";
            var stringToSign = string.Join('\n',
                "AWS4-HMAC-SHA256",
                amzDate,
                credentialScope,
                ToHex(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest))));

            var signingKey = GetSignatureKey(_secretKey, dateStamp, Region, Service);
            var signature = ToHex(HmacSha256(signingKey, stringToSign));

            request.Headers.Authorization = AuthenticationHeaderValue.Parse(
                $"AWS4-HMAC-SHA256 Credential={_accessKey}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}");
        }

        private string NormalizeObjectPath(string pathOrUrl)
        {
            var trimmed = pathOrUrl.Trim().TrimStart('/');

            if (trimmed.StartsWith(_publicBaseUrl, StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed.Substring(_publicBaseUrl.Length).TrimStart('/');

            var storagePrefix = $"https://{_storageHost}/{_bucketName}/";
            if (trimmed.StartsWith(storagePrefix, StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed.Substring(storagePrefix.Length).TrimStart('/');

            return trimmed;
        }

        private static string EncodeObjectPath(string objectPath)
        {
            var segments = objectPath
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString);

            return string.Join("/", segments);
        }

        private static string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".m3u8" => "application/vnd.apple.mpegurl",
                ".ts" => "video/mp2t",
                ".m4s" => "video/iso.segment",
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                ".mkv" => "video/x-matroska",
                _ => "application/octet-stream"
            };
        }

        private static byte[] GetSignatureKey(string key, string dateStamp, string regionName, string serviceName)
        {
            var kDate = HmacSha256(Encoding.UTF8.GetBytes("AWS4" + key), dateStamp);
            var kRegion = HmacSha256(kDate, regionName);
            var kService = HmacSha256(kRegion, serviceName);
            return HmacSha256(kService, "aws4_request");
        }

        private static byte[] HmacSha256(byte[] key, string data)
        {
            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        private static string ToHex(byte[] bytes)
        {
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
