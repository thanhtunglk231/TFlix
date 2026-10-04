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
        Task<CResponseMessage> CompleteMp4UploadAsync(Guid uploadId, CompleteMp4VideoUploadDto request);
        Task<CResponseMessage> CancelMp4UploadAsync(Guid uploadId);
        Task<ApiResponse<SourceTableWrapper>> get_all();
        Task<CResponseMessage> uppdate_VideoSoure(decimal sourceId, IFormFile file, [FromBody] UpdateVideoSourceInputDto meta);
    }
}