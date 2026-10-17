using CoreLib.Dtos.VideSoure;
using CoreLib.Models;
using Microsoft.AspNetCore.Mvc;
using WebBrowser.Models;
using WebBrowser.Models.Episode;
using WebBrowser.Models.VideoSoure;

namespace WebBrowser.Services.Interfaces
{
    public interface IVideoSoureService
    {
        Task<CResponseMessage> add_VideoSoure(IFormFile? file, AddVideoSourceInputDto addVideoSourceDto);
        Task<CResponseMessage> add_HlsVideoSource(
            IFormFile playlist,
            IReadOnlyList<IFormFile> segments,
            AddVideoSourceInputDto addVideoSourceDto,
            decimal? sourceId = null,
            string? oldStreamUrl = null);
        Task<CResponseMessage> UploadMp4ChunkAsync(Guid uploadId, int chunkIndex, int totalChunks, string fileFingerprint, IFormFile chunk);
        Task<Mp4UploadStatusDto> GetMp4UploadStatusAsync(Guid uploadId, string fileFingerprint);
        Task<CompleteMp4UploadResultDto> CompleteMp4UploadAsync(Guid uploadId, CompleteMp4VideoUploadDto request);
        Task<CResponseMessage> CancelMp4UploadAsync(Guid uploadId);
        Task<CResponseMessage> UploadBrowserHlsChunkAsync(Guid uploadId, Guid fileId, string fileName, long fileSize, int chunkIndex, int totalChunks, string checksum, IFormFile chunk);
        Task<BrowserHlsUploadStatusDto> GetBrowserHlsUploadStatusAsync(Guid uploadId);
        Task<BrowserHlsUploadStatusDto> GetBrowserHlsUploadStatusAsync(Guid uploadId, string bearerToken);
        Task<CResponseMessage> CompleteBrowserHlsUploadAsync(Guid uploadId, CompleteBrowserHlsUploadDto request);
        Task<CResponseMessage> CompleteBrowserHlsUploadAsync(Guid uploadId, CompleteBrowserHlsUploadDto request, string bearerToken);
        Task<CResponseMessage> CancelBrowserHlsUploadAsync(Guid uploadId);
        Task<ApiResponse<SourceTableWrapper>> get_all(int page = 1, int pageSize = 10, string? search = null,
            string? sourceType = null, long? movieId = null, long? episodeId = null, string? quality = null);
        Task<CResponseMessage> uppdate_VideoSoure(decimal sourceId, IFormFile? file, UpdateVideoSourceInputDto meta);
        Task<CResponseMessage> Delete_video_source(decimal sourceId);
    }
}
