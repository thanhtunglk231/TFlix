using CoreLib.Dtos.Subtitle;

namespace Server.Services.Subtitles;

public interface IGeminiSubtitleService
{
    Task<IReadOnlyList<GeminiSourceSubtitleCue>> TranscribeVietnameseAsync(
        byte[] audio,
        string mimeType,
        double timelineOffsetSeconds,
        double chunkDurationSeconds,
        CancellationToken cancellationToken = default);
}
