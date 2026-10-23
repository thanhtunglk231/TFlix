using System.Threading.Tasks;
using CoreLib.Dtos.Subtitle;
using CoreLib.Models;
using WebBrowser.Models;
using WebBrowser.Models.Subtitle;

namespace WebBrowser.Services.Interfaces
{
    public interface ISubtitleService
    {
        Task<ApiResponse<SubtitleTableWrapper>> GetBySourceAsync(long sourceId);
        Task<CResponseMessage> GenerateAsync(long sourceId);
        Task<BrowserSubtitleJobDto> StartBrowserJobAsync(BrowserSubtitleJobRequestDto request);
        Task<BrowserSubtitleJobDto> GetBrowserJobAsync(Guid jobId);
        Task<BrowserSubtitleChunkResultDto> UploadBrowserChunkAsync(Guid jobId, int chunkIndex,
            double offsetSeconds, double durationSeconds, IFormFile audio);
        Task<BrowserSubtitleJobDto> CompleteBrowserJobAsync(Guid jobId);
        Task<SubtitleDraftDto> GetDraftAsync(Guid jobId);
        Task<SubtitleDraftDto> UpdateDraftAsync(Guid jobId, UpdateSubtitleDraftDto request);
        Task<BrowserSubtitleJobDto> ApproveAsync(Guid jobId);
        Task<CResponseMessage> SaveSourceSubtitlesAsync(long sourceId, List<GeminiSourceSubtitleCue> cues);
        Task<GenerateSubtitleResponseDto?> GetSubtitlesAsync(long? movieId, long? episodeId, string? title = null, string? overview = null, double? durationSeconds = null);
        Task<GenerateSubtitleResponseDto?> GenerateSubtitlesAsync(GenerateSubtitleRequestDto request);
        Task<string?> GetWebVttAsync(long? movieId, long? episodeId, string lang);
    }
}
