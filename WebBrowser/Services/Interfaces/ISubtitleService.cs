using System.Threading.Tasks;
using CoreLib.Dtos.Subtitle;

namespace WebBrowser.Services.Interfaces
{
    public interface ISubtitleService
    {
        Task<GenerateSubtitleResponseDto?> GetSubtitlesAsync(long? movieId, long? episodeId, string? title = null, string? overview = null, double? durationSeconds = null);
        Task<GenerateSubtitleResponseDto?> GenerateSubtitlesAsync(GenerateSubtitleRequestDto request);
        Task<string?> GetWebVttAsync(long? movieId, long? episodeId, string lang);
    }
}
