using Amazon.S3;
using Amazon.S3.Model;
using CoreLib.Models;
using Microsoft.Extensions.Options;

namespace Server.CloudFlareServices
{
    public class R2Service : IR2Service
    {
        private readonly CloudflareR2Options _options;
        private readonly IAmazonS3 _s3Client;

        public R2Service(IOptions<CloudflareR2Options> options)
        {
            _options = options.Value;

            var config = new AmazonS3Config
            {
                ServiceURL = $"https://{_options.AccountId}.r2.cloudflarestorage.com",
                ForcePathStyle = true,
                AuthenticationRegion = "auto"
            };

            _s3Client = new AmazonS3Client(
                _options.AccessKey,
                _options.SecretKey,
                config
            );
        }

        public async Task<string?> UploadFileAsync(IFormFile file, string objectPath)
        {
            if (file == null || file.Length == 0)
                return null;

            objectPath = NormalizeObjectPath(objectPath);

            var contentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? GetContentTypeFromExtension(file.FileName)
                : file.ContentType;

            await using var stream = file.OpenReadStream();

            var request = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = objectPath,
                InputStream = stream,
                ContentType = contentType,
                AutoCloseStream = false,
                DisablePayloadSigning = true
            };

            await _s3Client.PutObjectAsync(request);

            return BuildPublicUrl(objectPath);
        }

        public async Task<bool> DeleteFileAsync(string fileUrlOrObjectPath)
        {
            if (string.IsNullOrWhiteSpace(fileUrlOrObjectPath))
                return false;

            var objectPath = ExtractObjectPath(fileUrlOrObjectPath);
            if (string.IsNullOrWhiteSpace(objectPath))
                return false;

            var request = new DeleteObjectRequest
            {
                BucketName = _options.BucketName,
                Key = objectPath
            };

            await _s3Client.DeleteObjectAsync(request);
            return true;
        }

        private string BuildPublicUrl(string objectPath)
        {
            var baseUrl = _options.PublicBaseUrl.TrimEnd('/');
            return $"{baseUrl}/{objectPath}";
        }

        private string ExtractObjectPath(string fileUrlOrObjectPath)
        {
            var input = fileUrlOrObjectPath.Trim();

            if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
                return NormalizeObjectPath(input);

            var publicBase = _options.PublicBaseUrl.TrimEnd('/');

            if (input.StartsWith(publicBase, StringComparison.OrdinalIgnoreCase))
            {
                var relative = input.Substring(publicBase.Length).TrimStart('/');
                return NormalizeObjectPath(Uri.UnescapeDataString(relative));
            }

            return NormalizeObjectPath(uri.AbsolutePath.TrimStart('/'));
        }

        private static string NormalizeObjectPath(string path)
        {
            return path.Replace("\\", "/").TrimStart('/');
        }

        private static string GetContentTypeFromExtension(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();

            return ext switch
            {
                ".m3u8" => "application/vnd.apple.mpegurl",
                ".ts" => "video/mp2t",
                ".mp4" => "video/mp4",
                ".m4s" => "video/iso.segment",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }
    }
}