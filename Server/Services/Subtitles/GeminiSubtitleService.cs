using System.Text.Json;
using CoreLib.Dtos.Subtitle;
using Server.Services.OpenAI;

namespace Server.Services.Subtitles;

public sealed class GeminiSubtitleService : IGeminiSubtitleService
{
    private const int MaxAudioBytes = 12 * 1024 * 1024;
    private static readonly SemaphoreSlim ConcurrencyGate = new(2, 2);
    private readonly IOpenAiClient _openAi;
    private readonly ILogger<GeminiSubtitleService> _logger;

    public GeminiSubtitleService(IOpenAiClient openAi, ILogger<GeminiSubtitleService> logger)
    {
        _openAi = openAi;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GeminiSourceSubtitleCue>> TranscribeVietnameseAsync(
        byte[] audio, string mimeType, double timelineOffsetSeconds, double chunkDurationSeconds,
        CancellationToken cancellationToken = default)
    {
        if (!_openAi.IsConfigured)
            throw new InvalidOperationException("OpenAI API chưa được cấu hình.");
        if (audio.Length == 0 || audio.Length > MaxAudioBytes)
            throw new InvalidOperationException("Audio chunk rỗng hoặc vượt quá 12 MiB.");
        if (mimeType is not ("audio/mpeg" or "audio/mp3" or "audio/mp4" or "audio/wav" or "audio/webm"))
            throw new InvalidOperationException("Định dạng audio chunk không được hỗ trợ.");

        await ConcurrencyGate.WaitAsync(cancellationToken);
        try
        {
            var prompt = "Transcribe only spoken dialogue. Preserve names and punctuation. Return timestamped segments.";

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var json = await _openAi.TranscribeAudioAsync(audio, mimeType, prompt, cancellationToken);
                var (success, cues) = ParseAndValidate(json, timelineOffsetSeconds, chunkDurationSeconds);
                if (success) return cues;
                if (attempt < 3)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), cancellationToken);
                }
            }

            _logger.LogWarning("Không trích xuất được phụ đề cho audio chunk tại offset {Offset:F1}s (duration {Duration:F1}s).", timelineOffsetSeconds, chunkDurationSeconds);
            throw new InvalidOperationException("OpenAI không trả về phụ đề hợp lệ. Vui lòng thử lại đoạn âm thanh này.");
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
            
            var raw = JsonSerializer.Deserialize<List<GeminiSourceSubtitleCue>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (raw == null)
            {
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.TryGetProperty("segments", out var segments) && segments.ValueKind == JsonValueKind.Array)
                    raw = segments.EnumerateArray().Select((segment, index) => new GeminiSourceSubtitleCue
                    {
                        StartSeconds = segment.TryGetProperty("start", out var start) ? start.GetDouble() : index * 3,
                        EndSeconds = segment.TryGetProperty("end", out var end) ? end.GetDouble() : (index + 1) * 3,
                        Vi = segment.TryGetProperty("text", out var text) ? text.GetString() : null,
                        En = segment.TryGetProperty("text", out var english) ? english.GetString() : null
                    }).ToList();
            }
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
