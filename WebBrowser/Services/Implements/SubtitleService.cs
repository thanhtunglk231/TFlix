using System;
using System.Threading.Tasks;
using CoreLib.Dtos.Subtitle;
using WebBrowser.Services.HttpSevice.Interfaces;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Services.Implements
{
    public class SubtitleService : ISubtitleService
    {
        private readonly IHttpService _http;

        public SubtitleService(IHttpService http)
        {
            _http = http;
        }

        public async Task<GenerateSubtitleResponseDto?> GetSubtitlesAsync(
            long? movieId,
            long? episodeId,
            string? title = null,
            string? overview = null,
            double? durationSeconds = null)
        {
            var url = $"/api/Subtitles?movieId={movieId}&episodeId={episodeId}&title={Uri.EscapeDataString(title ?? "")}&overview={Uri.EscapeDataString(overview ?? "")}&durationSeconds={durationSeconds ?? 0}";
            try
            {
                return await _http.GetAsync<GenerateSubtitleResponseDto>(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[SubtitleService.GetSubtitlesAsync] Error: " + ex.Message);
                return null;
            }
        }

        public async Task<GenerateSubtitleResponseDto?> GenerateSubtitlesAsync(GenerateSubtitleRequestDto request)
        {
            try
            {
                return await _http.PostLongRunningAsync<GenerateSubtitleResponseDto>("/api/Subtitles/generate", request);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[SubtitleService.GenerateSubtitlesAsync] Error: " + ex.Message);
                return null;
            }
        }

        public async Task<string?> GetWebVttAsync(long? movieId, long? episodeId, string lang)
        {
            var url = $"/api/Subtitles/vtt?movieId={movieId}&episodeId={episodeId}&lang={Uri.EscapeDataString(lang)}";
            try
            {
                return await _http.GetRawJsonAsync(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[SubtitleService.GetWebVttAsync] Error: " + ex.Message);
                return null;
            }
        }
    }
}
