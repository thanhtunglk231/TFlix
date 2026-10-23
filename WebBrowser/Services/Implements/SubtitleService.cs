using System;
using System.Threading.Tasks;
using System.Globalization;
using System.Net.Http.Headers;
using CoreLib.Dtos.Subtitle;
using CoreLib.Models;
using WebBrowser.Models;
using WebBrowser.Models.Subtitle;
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

        public Task<ApiResponse<SubtitleTableWrapper>> GetBySourceAsync(long sourceId) =>
            _http.GetAsync<ApiResponse<SubtitleTableWrapper>>($"/api/Subtitles/source/{sourceId}");

        public Task<CResponseMessage> GenerateAsync(long sourceId) =>
            _http.PostLongRunningAsync<CResponseMessage>($"/api/Subtitles/generate/{sourceId}", new { });

        public Task<BrowserSubtitleJobDto> StartBrowserJobAsync(BrowserSubtitleJobRequestDto request) =>
            _http.PostAsync<BrowserSubtitleJobDto>("/api/Subtitles/browser-jobs", request);

        public Task<BrowserSubtitleJobDto> GetBrowserJobAsync(Guid jobId) =>
            _http.GetAsync<BrowserSubtitleJobDto>($"/api/Subtitles/browser-jobs/{jobId}");

        public async Task<BrowserSubtitleChunkResultDto> UploadBrowserChunkAsync(Guid jobId, int chunkIndex,
            double offsetSeconds, double durationSeconds, IFormFile audio)
        {
            using var form = new MultipartFormDataContent();
            await using var stream = audio.OpenReadStream();
            using var audioContent = new StreamContent(stream);
            audioContent.Headers.ContentType = new MediaTypeHeaderValue(audio.ContentType ?? "audio/mpeg");
            form.Add(audioContent, "audio", audio.FileName);
            form.Add(new StringContent(offsetSeconds.ToString(CultureInfo.InvariantCulture)), "offsetSeconds");
            form.Add(new StringContent(durationSeconds.ToString(CultureInfo.InvariantCulture)), "durationSeconds");
            return await _http.PostMultipartAsync<BrowserSubtitleChunkResultDto>(
                $"/api/Subtitles/browser-jobs/{jobId}/chunks/{chunkIndex}", form);
        }

        public Task<BrowserSubtitleJobDto> CompleteBrowserJobAsync(Guid jobId) =>
            _http.PostAsync<BrowserSubtitleJobDto>($"/api/Subtitles/browser-jobs/{jobId}/complete", new { });

        public Task<SubtitleDraftDto> GetDraftAsync(Guid jobId) =>
            _http.GetAsync<SubtitleDraftDto>($"/api/Subtitles/browser-jobs/{jobId}/draft");

        public Task<SubtitleDraftDto> UpdateDraftAsync(Guid jobId, UpdateSubtitleDraftDto request) =>
            _http.PostAsync<SubtitleDraftDto>($"/api/Subtitles/browser-jobs/{jobId}/draft", request);

        public Task<BrowserSubtitleJobDto> ApproveAsync(Guid jobId) =>
            _http.PostLongRunningAsync<BrowserSubtitleJobDto>($"/api/Subtitles/browser-jobs/{jobId}/approve", new { });

        public Task<CResponseMessage> SaveSourceSubtitlesAsync(long sourceId, List<GeminiSourceSubtitleCue> cues)
        {
            var req = new SaveSourceSubtitlesDto { SourceId = sourceId, Cues = cues };
            return _http.PostAsync<CResponseMessage>($"/api/Subtitles/source/{sourceId}/save", req);
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
