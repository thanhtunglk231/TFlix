using System.Text.Json;
using CoreLib.Dtos.Subtitle;
using Server.Services.Gemini;

namespace Server.Services.Subtitles;

public sealed class GeminiSubtitleService : IGeminiSubtitleService
{
    private const int MaxAudioBytes = 12 * 1024 * 1024;
    private static readonly SemaphoreSlim ConcurrencyGate = new(2, 2);
    private readonly IGeminiClient _gemini;
    private readonly ILogger<GeminiSubtitleService> _logger;

    public GeminiSubtitleService(IGeminiClient gemini, ILogger<GeminiSubtitleService> logger)
    {
        _gemini = gemini;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GeminiSourceSubtitleCue>> TranscribeVietnameseAsync(
        byte[] audio, string mimeType, double timelineOffsetSeconds, double chunkDurationSeconds,
        CancellationToken cancellationToken = default)
    {
        if (!_gemini.IsConfigured)
            throw new InvalidOperationException("Gemini API chưa được cấu hình.");
        if (audio.Length == 0 || audio.Length > MaxAudioBytes)
            throw new InvalidOperationException("Audio chunk rỗng hoặc vượt quá 12 MiB.");
        if (mimeType is not ("audio/mpeg" or "audio/mp3" or "audio/mp4" or "audio/wav" or "audio/webm"))
            throw new InvalidOperationException("Định dạng audio chunk không được hỗ trợ.");

        await ConcurrencyGate.WaitAsync(cancellationToken);
        try
        {
            var prompt = $$"""
                Nghe đoạn âm thanh phim dài tối đa {{chunkDurationSeconds:F3}} giây.
                Âm thanh có thể là tiếng Anh, tiếng Hàn hoặc tiếng Nhật. Chỉ ghi lời thoại thực sự nghe được,
                tạo hai bản dịch tự nhiên: Tiếng Việt trong trường "vi" và Tiếng Anh trong trường "en".
                Giữ nguyên tên riêng và ý nghĩa. Không suy đoán hoặc tạo lời thoại.
                Bỏ qua nhạc và tiếng động không có lời. Trả về JSON array duy nhất, không markdown:
                [{"startSeconds":0.0,"endSeconds":2.5,"vi":"Nội dung tiếng Việt","en":"English subtitle"}].
                Thời gian phải tương đối trong chunk, từ 0 đến {{chunkDurationSeconds:F3}}; mỗi câu ngắn, dễ đọc.
                """;

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var json = await _gemini.GenerateWithAudioAsync(prompt, audio, mimeType, cancellationToken);
                var (success, cues) = ParseAndValidate(json, timelineOffsetSeconds, chunkDurationSeconds);
                if (success) return cues;
                if (attempt < 3)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), cancellationToken);
                }
            }

            _logger.LogWarning("Không trích xuất được phụ đề cho audio chunk tại offset {Offset:F1}s (duration {Duration:F1}s). Đánh dấu chunk rỗng để tiếp tục tiến trình.", timelineOffsetSeconds, chunkDurationSeconds);
            return new List<GeminiSourceSubtitleCue>();
        }
        finally
        {
            ConcurrencyGate.Release();
        }
    }

    private (bool Success, List<GeminiSourceSubtitleCue> Cues) ParseAndValidate(string? json, double offset, double duration)
    {
        if (string.IsNullOrWhiteSpace(json)) return (false, new());
        try
        {
            json = json.Replace("```json", "", StringComparison.OrdinalIgnoreCase).Replace("```", "").Trim();
            
            var startIdx = json.IndexOf('[');
            var endIdx = json.LastIndexOf(']');
            if (startIdx >= 0 && endIdx > startIdx)
            {
                json = json.Substring(startIdx, endIdx - startIdx + 1);
            }

            var raw = JsonSerializer.Deserialize<List<GeminiSourceSubtitleCue>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (raw == null) return (true, new());

            var result = new List<GeminiSourceSubtitleCue>();
            double previousEnd = offset;
            foreach (var cue in raw.OrderBy(item => item.StartSeconds))
            {
                if (string.IsNullOrWhiteSpace(cue.Vi) && string.IsNullOrWhiteSpace(cue.En)) continue;
                var viText = !string.IsNullOrWhiteSpace(cue.Vi) ? cue.Vi : cue.En;
                var enText = !string.IsNullOrWhiteSpace(cue.En) ? cue.En : cue.Vi;
                var start = Math.Clamp(cue.StartSeconds, 0, duration) + offset;
                var end = Math.Clamp(cue.EndSeconds, 0, duration) + offset;
                start = Math.Max(start, previousEnd);
                if (end <= start || end - start > 20) continue;
                result.Add(new GeminiSourceSubtitleCue
                {
                    StartSeconds = start,
                    EndSeconds = end,
                    Vi = NormalizeText(viText!),
                    En = NormalizeText(enText!)
                });
                previousEnd = end;
            }
            return (true, result);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Gemini subtitle response was not valid JSON: {Json}", json);
            return (false, new());
        }
    }

    private static string NormalizeText(string value) =>
        string.Join(' ', value.Replace('\r', ' ').Replace('\n', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim();
}
