using CoreLib.Dtos.Subtitle;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Http;
using Server.Services.Gemini;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Server.Services.Subtitles;

public sealed class SubtitleGenerator : ISubtitleGenerator
{
    private const double ChunkSeconds = 600;
    private readonly ICVideoSoure _sources;
    private readonly ICSubtitle _subtitles;
    private readonly IGeminiClient _gemini;
    private readonly ICloudflareR2Service _r2;
    private readonly ILogger<SubtitleGenerator> _logger;
    private readonly string _ffmpeg;
    private readonly string _ffprobe;

    public SubtitleGenerator(ICVideoSoure sources, ICSubtitle subtitles, IGeminiClient gemini,
        ICloudflareR2Service r2, IConfiguration configuration, ILogger<SubtitleGenerator> logger)
    {
        _sources = sources; _subtitles = subtitles; _gemini = gemini; _r2 = r2; _logger = logger;
        _ffmpeg = configuration["FFmpeg:Path"] ?? "ffmpeg";
        var directory = Path.GetDirectoryName(_ffmpeg);
        _ffprobe = string.IsNullOrWhiteSpace(directory) ? "ffprobe" : Path.Combine(directory, "ffprobe.exe");
    }

    public async Task<CResponseMessage> GenerateAsync(long sourceId, CancellationToken token = default)
    {
        if (!_gemini.IsConfigured) return Fail("400", "Gemini API chưa được cấu hình.");
        var response = _sources.GetBySourceId(sourceId);
        if (!response.Success || response.Data is not DataSet ds || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
            return Fail("404", "Không tìm thấy nguồn video.");
        var row = ds.Tables[0].Rows[0];
        var url = Read(row, "stream_url", "StreamUrl");
        if (string.IsNullOrWhiteSpace(url)) return Fail("400", "Nguồn video không có Stream URL.");

        var temp = Path.Combine(Path.GetTempPath(), "TFlix", "Subtitles", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var uploaded = new List<string>();
        try
        {
            var duration = await ProbeDurationAsync(url, token);
            if (duration <= 0) return Fail("400", "Không đọc được thời lượng video.");
            var cues = new List<GeminiSourceSubtitleCue>();
            for (double offset = 0; offset < duration; offset += ChunkSeconds)
            {
                var audioPath = Path.Combine(temp, $"audio-{offset:0}.mp3");
                await ExtractAudioAsync(url, offset, Math.Min(ChunkSeconds, duration - offset), audioPath, token);
                var prompt = $"""
                    Hãy nghe đoạn âm thanh phim bắt đầu tại giây {offset.ToString("F3", CultureInfo.InvariantCulture)}.
                    Tạo phụ đề chính xác theo lời thoại, bỏ tiếng động và nhạc không có lời. Trả về JSON array duy nhất,
                    không markdown, mỗi phần tử gồm startSeconds, endSeconds, vi, en. Mốc thời gian phải là thời gian
                    tuyệt đối của toàn bộ phim (đã cộng {offset.ToString("F3", CultureInfo.InvariantCulture)} giây).
                    vi là tiếng Việt tự nhiên; en là tiếng Anh tự nhiên. Mỗi câu ngắn, dễ đọc, không chồng thời gian.
                    """;
                var json = await _gemini.GenerateWithAudioAsync(prompt, await File.ReadAllBytesAsync(audioPath, token), "audio/mpeg", token);
                cues.AddRange(ParseCues(json, offset, duration));
            }
            if (cues.Count == 0) return Fail("502", "Gemini không nhận dạng được lời thoại trong video.");

            foreach (var language in new[] { "vi", "en" })
            {
                var path = Path.Combine(temp, $"{language}.vtt");
                await File.WriteAllTextAsync(path, BuildVtt(cues, language), new UTF8Encoding(false), token);
                var objectPath = $"subtitles/{sourceId}/{language}-{Guid.NewGuid():N}.vtt";
                await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var file = new FormFile(stream, 0, stream.Length, "file", Path.GetFileName(path))
                    { Headers = new HeaderDictionary(), ContentType = "text/vtt; charset=utf-8" };
                var publicUrl = await _r2.UploadFileAsync(file, objectPath);
                if (string.IsNullOrWhiteSpace(publicUrl)) throw new InvalidOperationException($"Không upload được phụ đề {language}.");
                uploaded.Add(publicUrl);
                var saved = await _subtitles.UpsertAsync(new CreateSubtitleDto
                    { SourceId = sourceId, LanguageCode = language, Url = publicUrl, Format = "VTT", IsDefault = language == "vi" });
                if (!saved.Success) throw new InvalidOperationException(saved.message);
            }
            return new CResponseMessage { Success = true, code = "200", message = "Đã tạo phụ đề tiếng Việt và tiếng Anh bằng Gemini." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Generate subtitles failed for source {SourceId}", sourceId);
            foreach (var urlToDelete in uploaded) await _r2.DeleteFileAsync(urlToDelete);
            return Fail("500", "Không tạo được phụ đề: " + ex.Message);
        }
        finally { try { Directory.Delete(temp, true); } catch { } }
    }

    private async Task<double> ProbeDurationAsync(string url, CancellationToken token)
    {
        var output = await RunAsync(_ffprobe, new[] { "-v", "error", "-show_entries", "format=duration", "-of", "default=noprint_wrappers=1:nokey=1", url }, token);
        return double.TryParse(output.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }
    private Task ExtractAudioAsync(string url, double offset, double length, string output, CancellationToken token) =>
        RunAsync(_ffmpeg, new[] { "-hide_banner", "-loglevel", "error", "-y", "-ss", offset.ToString("F3", CultureInfo.InvariantCulture), "-i", url,
            "-t", length.ToString("F3", CultureInfo.InvariantCulture), "-vn", "-ac", "1", "-ar", "16000", "-b:a", "32k", output }, token);
    private static List<GeminiSourceSubtitleCue> ParseCues(string? json, double offset, double duration)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            json = json.Replace("```json", "", StringComparison.OrdinalIgnoreCase).Replace("```", "").Trim();
            return (JsonSerializer.Deserialize<List<GeminiSourceSubtitleCue>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new())
                .Where(x => x.StartSeconds >= Math.Max(0, offset - 1) && x.EndSeconds > x.StartSeconds && x.EndSeconds <= duration + 1
                    && (!string.IsNullOrWhiteSpace(x.Vi) || !string.IsNullOrWhiteSpace(x.En))).ToList();
        }
        catch { return new(); }
    }
    private static string BuildVtt(IEnumerable<GeminiSourceSubtitleCue> cues, string language)
    {
        var builder = new StringBuilder("WEBVTT\n\n"); var index = 0;
        foreach (var cue in cues.OrderBy(x => x.StartSeconds))
        {
            var text = language == "vi" ? cue.Vi : cue.En;
            if (string.IsNullOrWhiteSpace(text)) continue;
            builder.AppendLine((++index).ToString(CultureInfo.InvariantCulture));
            builder.Append(FormatTime(cue.StartSeconds)).Append(" --> ").AppendLine(FormatTime(cue.EndSeconds));
            builder.AppendLine(text.Trim().Replace("\r", " ").Replace("\n", " ")).AppendLine();
        }
        return builder.ToString();
    }
    private static string FormatTime(double seconds) => TimeSpan.FromSeconds(Math.Max(0, seconds)).ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture);
    private static async Task<string> RunAsync(string executable, IEnumerable<string> arguments, CancellationToken token)
    {
        var start = new ProcessStartInfo { FileName = executable, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException($"Không khởi động được {executable}.");
        var stdout = process.StandardOutput.ReadToEndAsync(token); var stderr = process.StandardError.ReadToEndAsync(token);
        await process.WaitForExitAsync(token);
        if (process.ExitCode != 0) throw new InvalidOperationException((await stderr).Trim());
        return await stdout;
    }
    private static string? Read(DataRow row, params string[] names)
    { foreach (var name in names) if (row.Table.Columns.Contains(name) && row[name] != DBNull.Value) return row[name]?.ToString(); return null; }
    private static CResponseMessage Fail(string code, string message) => new() { Success = false, code = code, message = message };
}
