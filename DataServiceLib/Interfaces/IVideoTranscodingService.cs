namespace DataServiceLib.Interfaces
{
    public interface IVideoTranscodingService
    {
        Task<string> CreateHlsAsync(
            string sourcePath,
            string outputDirectory,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default);
    }
}
