using CoreLib.Dtos.Subtitle;
using Microsoft.AspNetCore.Http;

namespace Server.Services.Subtitles;

public interface IBrowserSubtitleJobService
{
    Task<BrowserSubtitleJobDto> StartAsync(BrowserSubtitleJobRequestDto request, string ownerId, CancellationToken cancellationToken);
    Task<BrowserSubtitleJobDto?> GetAsync(Guid jobId, string ownerId, CancellationToken cancellationToken);
    Task<BrowserSubtitleChunkResultDto> ProcessChunkAsync(Guid jobId, int chunkIndex, double offsetSeconds,
        double durationSeconds, IFormFile audio, string ownerId, CancellationToken cancellationToken);
    Task<BrowserSubtitleJobDto> CompleteAsync(Guid jobId, string ownerId, CancellationToken cancellationToken);
    Task<(byte[] Content, string ContentType)> DownloadHlsFileAsync(long sourceId, string fileUrl,
        CancellationToken cancellationToken);
    Task<SubtitleDraftDto> GetDraftAsync(Guid jobId, string ownerId, CancellationToken cancellationToken);
    Task<SubtitleDraftDto> UpdateDraftAsync(Guid jobId, UpdateSubtitleDraftDto request, string ownerId,
        CancellationToken cancellationToken);
    Task<BrowserSubtitleJobDto> ApproveAsync(Guid jobId, string ownerId, CancellationToken cancellationToken);
    Task<bool> SaveSourceSubtitlesAsync(long sourceId, List<GeminiSourceSubtitleCue> cues, CancellationToken cancellationToken);
}

