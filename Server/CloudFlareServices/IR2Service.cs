namespace Server.CloudFlareServices
{
    public interface IR2Service
    {
        Task<string?> UploadFileAsync(IFormFile file, string objectPath);
        Task<bool> DeleteFileAsync(string fileUrlOrObjectPath);
    }
}
