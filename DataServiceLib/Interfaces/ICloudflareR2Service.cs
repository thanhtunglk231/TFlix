using Microsoft.AspNetCore.Http;

namespace DataServiceLib.Interfaces
{
    public interface ICloudflareR2Service
    {
        Task<string?> UploadFileAsync(IFormFile file, string objectPath);
        Task<bool> DeleteFileAsync(string pathOrUrl);
    }
}
