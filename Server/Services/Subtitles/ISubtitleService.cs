using System.Threading;
using System.Threading.Tasks;
using CoreLib.Dtos.Subtitle;

namespace Server.Services.Subtitles
{
    public interface ISubtitleService
    {
        Task<GenerateSubtitleResponseDto> GetOrGenerateSubtitlesAsync(
            GenerateSubtitleRequestDto request,
            CancellationToken cancellationToken = default);

        Task<string> GetWebVttContentAsync(
            long? movieId,
            long? episodeId,
            string languageCode,
            CancellationToken cancellationToken = default);
    }
}
