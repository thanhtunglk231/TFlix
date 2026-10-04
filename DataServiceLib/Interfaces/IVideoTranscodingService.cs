namespace DataServiceLib.Interfaces
{
    public interface IVideoTranscodingService
    {
        Task<string> CreateHlsAsync(string sourcePath, string outputDirectory, CancellationToken cancellationToken = default);
    }
}