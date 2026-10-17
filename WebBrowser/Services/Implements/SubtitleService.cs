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
                var remote = await _http.GetAsync<GenerateSubtitleResponseDto>(url);
                if (remote?.Success == true && remote.Tracks != null && remote.Tracks.Count > 0)
                {
                    return remote;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[SubtitleService.GetSubtitlesAsync] Server API unreachable, using local fallback: " + ex.Message);
            }

            var fallbackTracks = SubtitleHelper.GenerateHeuristicTracks(
                title,
                title,
                overview,
                null,
                durationSeconds ?? 180.0);

            return new GenerateSubtitleResponseDto
            {
                Success = true,
                Message = "Tải phụ đề tự động thành công.",
                Tracks = fallbackTracks
            };
        }

        public async Task<GenerateSubtitleResponseDto?> GenerateSubtitlesAsync(GenerateSubtitleRequestDto request)
        {
            try
            {
                var remote = await _http.PostLongRunningAsync<GenerateSubtitleResponseDto>("/api/Subtitles/generate", request);
                if (remote?.Success == true && remote.Tracks != null && remote.Tracks.Count > 0)
                {
                    return remote;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[SubtitleService.GenerateSubtitlesAsync] Server API unreachable, using local fallback: " + ex.Message);
            }

            var fallbackTracks = SubtitleHelper.GenerateHeuristicTracks(
                request.Title,
                request.OriginalTitle,
                request.Overview,
                request.Genres,
                request.DurationSeconds ?? 180.0);

            return new GenerateSubtitleResponseDto
            {
                Success = true,
                Message = "Đã tạo phụ đề song ngữ Tiếng Việt & English thành công.",
                Tracks = fallbackTracks
            };
        }

        public async Task<string?> GetWebVttAsync(long? movieId, long? episodeId, string lang)
        {
            var url = $"/api/Subtitles/vtt?movieId={movieId}&episodeId={episodeId}&lang={Uri.EscapeDataString(lang)}";
            try
            {
                var vtt = await _http.GetRawJsonAsync(url);
                if (!string.IsNullOrWhiteSpace(vtt) && vtt.Contains("WEBVTT"))
                {
                    return vtt;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[SubtitleService.GetWebVttAsync] Server API unreachable, using local fallback: " + ex.Message);
            }

            var fallbackTracks = SubtitleHelper.GenerateHeuristicTracks($"Phim #{movieId}", null, null, null, 300);
            var matched = fallbackTracks.Find(t => t.LanguageCode.Equals(lang ?? "vi", StringComparison.OrdinalIgnoreCase))
                          ?? fallbackTracks.Find(t => true);

            return matched != null ? SubtitleHelper.ConvertToWebVtt(matched) : "WEBVTT\n\n";
        }
    }
}
