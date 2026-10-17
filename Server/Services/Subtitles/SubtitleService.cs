using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CoreLib.Dtos.Chat;
using CoreLib.Dtos.Subtitle;
using Microsoft.Extensions.Logging;
using Server.Services.Gemini;

namespace Server.Services.Subtitles
{
    public class SubtitleService : ISubtitleService
    {
        private readonly IGeminiClient _geminiClient;
        private readonly ILogger<SubtitleService> _logger;
        private static readonly ConcurrentDictionary<string, List<SubtitleTrackDto>> _cache = new();

        public SubtitleService(
            IGeminiClient geminiClient,
            ILogger<SubtitleService> logger)
        {
            _geminiClient = geminiClient;
            _logger = logger;
        }

        public async Task<GenerateSubtitleResponseDto> GetOrGenerateSubtitlesAsync(
            GenerateSubtitleRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return new GenerateSubtitleResponseDto
                {
                    Success = false,
                    Message = "Yêu cầu không hợp lệ."
                };
            }

            var cacheKey = BuildCacheKey(request.MovieId, request.EpisodeId);

            if (!request.ForceRegenerate && _cache.TryGetValue(cacheKey, out var cachedTracks) && cachedTracks.Count > 0)
            {
                return new GenerateSubtitleResponseDto
                {
                    Success = true,
                    Message = "Tải phụ đề từ bộ nhớ đệm thành công.",
                    Tracks = cachedTracks
                };
            }

            List<SubtitleTrackDto>? tracks = null;
            var isGeminiUsed = false;

            if (_geminiClient.IsConfigured)
            {
                try
                {
                    tracks = await GenerateWithGeminiAsync(request, cancellationToken);
                    if (tracks != null && tracks.Count > 0)
                    {
                        isGeminiUsed = true;
                        _logger.LogInformation("Đã tạo phụ đề thành công bằng Gemini AI cho {CacheKey}", cacheKey);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi gọi Gemini AI sinh phụ đề cho {CacheKey}. Chuyển sang fallback.", cacheKey);
                }
            }

            // Fallback heuristic nếu Gemini không khả dụng hoặc lỗi
            if (tracks == null || tracks.Count == 0)
            {
                tracks = SubtitleHelper.GenerateHeuristicTracks(
                    request.Title,
                    request.OriginalTitle,
                    request.Overview,
                    request.Genres,
                    request.DurationSeconds ?? 180.0);
                _logger.LogInformation("Đã tạo phụ đề tự động liên tục theo cốt truyện cho {CacheKey}", cacheKey);
            }

            _cache[cacheKey] = tracks;

            return new GenerateSubtitleResponseDto
            {
                Success = true,
                Message = isGeminiUsed
                    ? "Đã tạo phụ đề song ngữ Tiếng Việt & English thành công bằng Gemini AI."
                    : "Đã tạo phụ đề song ngữ Tiếng Việt & English tự động theo phim.",
                Tracks = tracks
            };
        }

        public async Task<string> GetWebVttContentAsync(
            long? movieId,
            long? episodeId,
            string languageCode,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = BuildCacheKey(movieId, episodeId);
            if (!_cache.TryGetValue(cacheKey, out var tracks) || tracks.Count == 0)
            {
                var result = await GetOrGenerateSubtitlesAsync(new GenerateSubtitleRequestDto
                {
                    MovieId = movieId,
                    EpisodeId = episodeId,
                    Title = $"Phim #{movieId}",
                    DurationSeconds = 300
                }, cancellationToken);

                tracks = result.Tracks;
            }

            var lang = string.IsNullOrWhiteSpace(languageCode) ? "vi" : languageCode.Trim().ToLowerInvariant();
            var matchedTrack = tracks.FirstOrDefault(t => t.LanguageCode.Equals(lang, StringComparison.OrdinalIgnoreCase))
                               ?? tracks.FirstOrDefault();

            if (matchedTrack == null)
            {
                return "WEBVTT\n\n1\n00:00:00.500 --> 00:00:05.500\n[TFlix Streaming]\n";
            }

            return SubtitleHelper.ConvertToWebVtt(matchedTrack);
        }

        private async Task<List<SubtitleTrackDto>?> GenerateWithGeminiAsync(
            GenerateSubtitleRequestDto req,
            CancellationToken cancellationToken)
        {
            var title = req.Title ?? "Tác phẩm điện ảnh";
            var originalTitle = req.OriginalTitle ?? title;
            var overview = req.Overview ?? "Một bộ phim đặc sắc trên nền tảng TFlix.";
            var genres = req.Genres ?? "Điện ảnh, Tâm lý, Hành động";
            var duration = req.DurationSeconds.HasValue && req.DurationSeconds > 30 ? req.DurationSeconds.Value : 180.0;
            var durationMin = Math.Max(1.0, duration / 60.0);

            var systemInstruction = "Bạn là chuyên gia dịch thuật và biên soạn phụ đề phim chuyên nghiệp (Professional Subtitle Creator). Trả về JSON hợp lệ tuyệt đối, không chứa ký tự thừa ngoài JSON.";

            var prompt = $@"Hãy tạo kịch bản phụ đề điện ảnh chuẩn song ngữ TIẾNG VIỆT ('vi') và TIẾNG ANH ('en') cho video sau:
- Tên phim: {title}
- Tên gốc: {originalTitle}
- Thể loại: {genres}
- Cốt truyện: {overview}
- Thời lượng video: {duration:F0} giây (khoảng {durationMin:F1} phút).

Yêu cầu xuất ra cấu trúc JSON duy nhất:
{{
  ""subtitles"": [
    {{
      ""languageCode"": ""vi"",
      ""languageName"": ""Tiếng Việt"",
      ""cues"": [
        {{ ""start"": ""00:00:03.000"", ""end"": ""00:00:07.500"", ""text"": ""...câu thoại hoặc dẫn chuyện tiếng Việt..."" }},
        {{ ""start"": ""00:00:08.500"", ""end"": ""00:00:13.500"", ""text"": ""...câu thoại kế tiếp..."" }}
      ]
    }},
    {{
      ""languageCode"": ""en"",
      ""languageName"": ""English"",
      ""cues"": [
        {{ ""start"": ""00:00:03.000"", ""end"": ""00:00:07.500"", ""text"": ""...matching English dialogue or narration..."" }},
        {{ ""start"": ""00:00:08.500"", ""end"": ""00:00:13.500"", ""text"": ""...next English dialogue..."" }}
      ]
    }}
  ]
}}

QUY TẮC BẮT BUỘC:
1. Tạo từ 8 đến 14 đoạn thoại cho mỗi ngôn ngữ, trải đều từ đầu 00:00:03 đến gần cuối video ({FormatSecondsToWebVtt(Math.Max(10, duration - 5))}).
2. Thời gian bắt đầu và kết thúc phải đúng định dạng mm:ss.fff hoặc hh:mm:ss.fff.
3. Nội dung tiếng Việt và tiếng Anh phải ăn khớp thời gian (start & end tương đồng).
4. Câu thoại tự nhiên, giàu cảm xúc, phù hợp đúng thể loại phim.";

            var history = new List<ChatMessageDto>();
            var responseText = await _geminiClient.GenerateContentAsync(systemInstruction, history, prompt, cancellationToken);

            if (string.IsNullOrWhiteSpace(responseText))
                return null;

            return ParseGeminiResponse(responseText, duration);
        }

        private List<SubtitleTrackDto>? ParseGeminiResponse(string responseText, double maxDuration)
        {
            try
            {
                var cleanJson = ExtractJsonBlock(responseText);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var payload = JsonSerializer.Deserialize<GeminiSubtitlePayload>(cleanJson, options);

                if (payload?.Subtitles == null || payload.Subtitles.Count == 0)
                    return null;

                var result = new List<SubtitleTrackDto>();

                foreach (var st in payload.Subtitles)
                {
                    var langCode = st.LanguageCode?.ToLowerInvariant().Trim() ?? "vi";
                    var isVi = langCode.StartsWith("vi");
                    var langName = isVi ? "Tiếng Việt" : "English";

                    var track = new SubtitleTrackDto
                    {
                        LanguageCode = isVi ? "vi" : "en",
                        LanguageName = isVi ? "Tiếng Việt (AI Gemini)" : "English (AI Gemini)",
                        Format = "VTT",
                        IsDefault = isVi,
                        Cues = new List<SubtitleCueDto>()
                    };

                    int cueIndex = 1;
                    if (st.Cues != null)
                    {
                        foreach (var c in st.Cues)
                        {
                            if (string.IsNullOrWhiteSpace(c.Text)) continue;

                            var startSec = ParseTimeString(c.Start, (cueIndex - 1) * 8.0);
                            var endSec = ParseTimeString(c.End, startSec + 4.0);

                            if (endSec <= startSec) endSec = startSec + 4.0;
                            if (maxDuration > 10 && startSec >= maxDuration) break;

                            track.Cues.Add(new SubtitleCueDto
                            {
                                Index = cueIndex++,
                                Start = FormatSecondsToWebVtt(startSec),
                                End = FormatSecondsToWebVtt(Math.Min(endSec, maxDuration > 10 ? maxDuration : endSec)),
                                StartSeconds = Math.Round(startSec, 3),
                                EndSeconds = Math.Round(endSec, 3),
                                Text = c.Text.Trim()
                            });
                        }
                    }

                    if (track.Cues.Count > 0)
                    {
                        result.Add(track);
                    }
                }

                return result.Count > 0 ? result : null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi parse JSON phụ đề từ Gemini: {Text}", responseText);
                return null;
            }
        }

        private List<SubtitleTrackDto> GenerateHeuristicSubtitles(GenerateSubtitleRequestDto req)
        {
            var title = req.Title ?? "Tác phẩm điện ảnh";
            var originalTitle = req.OriginalTitle ?? title;
            var overview = req.Overview;
            var duration = req.DurationSeconds.HasValue && req.DurationSeconds > 30 ? req.DurationSeconds.Value : 180.0;

            var viCues = new List<(string text, double start, double end)>();
            var enCues = new List<(string text, double start, double end)>();

            var beats = new List<(string vi, string en)>
            {
                ($"[Âm nhạc mở đầu] Chào mừng đến với tác phẩm: {title}", $"[Opening Theme] Welcome to: {originalTitle}"),
                ("Trong một thế giới đầy biến động và những lựa chọn định mệnh...", "In a shifting world of crucial turning points..."),
                (!string.IsNullOrWhiteSpace(overview) ? overview : "Mỗi bước đi đều có thể thay đổi số phận của tất cả mọi người.", !string.IsNullOrWhiteSpace(overview) ? $"The story unfolds: {title}" : "Every decision carries weight and alters destiny."),
                ("Chúng ta không thể lùi bước vào thời khắc quan trọng này.", "We cannot turn back at this critical juncture."),
                ("Hãy tin tưởng vào những đồng đội đang kề vai sát cánh.", "Have faith in the companions fighting by your side."),
                ("Thời khắc quyết định đã đến. Tất cả chuẩn bị sẵn sàng!", "The defining moment is here. Everyone, stand ready!"),
                ("Dù kết quả thế nào, chúng ta đã chiến đấu hết mình.", "Whatever happens, we gave everything we had."),
                ("TFlix Streaming - Cảm ơn bạn đã thưởng thức tác phẩm này.", "TFlix Streaming - Thank you for watching.")
            };

            var step = Math.Max(6.0, (duration - 15.0) / beats.Count);
            var currentStart = 3.0;

            for (int i = 0; i < beats.Count; i++)
            {
                var cueDuration = Math.Min(5.5, step - 1.0);
                var cueEnd = currentStart + cueDuration;

                viCues.Add((beats[i].vi, currentStart, cueEnd));
                enCues.Add((beats[i].en, currentStart, cueEnd));

                currentStart += step;
                if (currentStart >= duration - 5) break;
            }

            var viTrack = new SubtitleTrackDto
            {
                LanguageCode = "vi",
                LanguageName = "Tiếng Việt (AI)",
                Format = "VTT",
                IsDefault = true,
                Cues = viCues.Select((c, idx) => new SubtitleCueDto
                {
                    Index = idx + 1,
                    Start = FormatSecondsToWebVtt(c.start),
                    End = FormatSecondsToWebVtt(c.end),
                    StartSeconds = Math.Round(c.start, 3),
                    EndSeconds = Math.Round(c.end, 3),
                    Text = c.text
                }).ToList()
            };

            var enTrack = new SubtitleTrackDto
            {
                LanguageCode = "en",
                LanguageName = "English (AI)",
                Format = "VTT",
                IsDefault = false,
                Cues = enCues.Select((c, idx) => new SubtitleCueDto
                {
                    Index = idx + 1,
                    Start = FormatSecondsToWebVtt(c.start),
                    End = FormatSecondsToWebVtt(c.end),
                    StartSeconds = Math.Round(c.start, 3),
                    EndSeconds = Math.Round(c.end, 3),
                    Text = c.text
                }).ToList()
            };

            return new List<SubtitleTrackDto> { viTrack, enTrack };
        }

        private static string ConvertTrackToWebVtt(SubtitleTrackDto track)
        {
            var sb = new StringBuilder();
            sb.AppendLine("WEBVTT");
            sb.AppendLine($"NOTE Subtitle Track: {track.LanguageName} ({track.LanguageCode}) - Created by TFlix AI Gemini");
            sb.AppendLine();

            foreach (var cue in track.Cues)
            {
                sb.AppendLine(cue.Index.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine($"{cue.Start} --> {cue.End}");
                sb.AppendLine(cue.Text);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string FormatSecondsToWebVtt(double seconds)
        {
            var ts = TimeSpan.FromSeconds(Math.Max(0, seconds));
            return string.Format(CultureInfo.InvariantCulture, "{0:D2}:{1:D2}:{2:D2}.{3:D3}",
                (int)ts.TotalHours, ts.Minutes, ts.Seconds, ts.Milliseconds);
        }

        private static double ParseTimeString(string? timeStr, double fallback)
        {
            if (string.IsNullOrWhiteSpace(timeStr)) return fallback;

            var clean = timeStr.Trim().Replace(',', '.');
            var parts = clean.Split(':');

            try
            {
                if (parts.Length == 3)
                {
                    var hours = double.Parse(parts[0], CultureInfo.InvariantCulture);
                    var minutes = double.Parse(parts[1], CultureInfo.InvariantCulture);
                    var seconds = double.Parse(parts[2], CultureInfo.InvariantCulture);
                    return (hours * 3600.0) + (minutes * 60.0) + seconds;
                }
                if (parts.Length == 2)
                {
                    var minutes = double.Parse(parts[0], CultureInfo.InvariantCulture);
                    var seconds = double.Parse(parts[1], CultureInfo.InvariantCulture);
                    return (minutes * 60.0) + seconds;
                }
                if (double.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var sec))
                {
                    return sec;
                }
            }
            catch { }

            return fallback;
        }

        private static string ExtractJsonBlock(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "{}";

            var match = Regex.Match(text, @"```(?:json)?\s*([\s\S]*?)\s*```", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                return text.Substring(start, end - start + 1);
            }

            return text.Trim();
        }

        private static string BuildCacheKey(long? movieId, long? episodeId)
        {
            return $"subtitles_{movieId?.ToString() ?? "0"}_{episodeId?.ToString() ?? "0"}";
        }
    }
}
