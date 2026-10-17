using CoreLib.Dtos.VideoHighlight;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Http;
using Server.Services.Gemini;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace Server.Services.VideoHighlights;

public sealed class VideoHighlightGenerator : IVideoHighlightGenerator
{
    private readonly ICVideoSoure _sources;
    private readonly ICVideoHighlight _highlights;
    private readonly IGeminiClient _gemini;
    private readonly ICloudflareR2Service _r2;
    private readonly ILogger<VideoHighlightGenerator> _logger;
    private readonly string _ffmpeg;
    private readonly string _ffprobe;

    public VideoHighlightGenerator(ICVideoSoure sources, ICVideoHighlight highlights, IGeminiClient gemini,
        ICloudflareR2Service r2, IConfiguration configuration, ILogger<VideoHighlightGenerator> logger)
    {
        _sources = sources; _highlights = highlights; _gemini = gemini; _r2 = r2; _logger = logger;
        _ffmpeg = configuration["FFmpeg:Path"] ?? "ffmpeg";
        var directory = Path.GetDirectoryName(_ffmpeg);
        _ffprobe = string.IsNullOrWhiteSpace(directory) ? "ffprobe" : Path.Combine(directory, "ffprobe.exe");
    }

    public async Task<CResponseMessage> GenerateAsync(long sourceId, CancellationToken cancellationToken = default)
    {
        if (!_gemini.IsConfigured)
            return Fail("400", "Gemini API chưa được cấu hình.");
        var sourceResponse = _sources.GetBySourceId(sourceId);
        if (!sourceResponse.Success || sourceResponse.Data is not DataSet ds || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
            return Fail("404", "Không tìm thấy nguồn video.");
        var row = ds.Tables[0].Rows[0];
        var url = Read(row, "stream_url", "StreamUrl");
        if (string.IsNullOrWhiteSpace(url)) return Fail("400", "Nguồn video không có Stream URL.");
        var contentTitle = Read(row, "movie_title", "episode_title", "title") ?? $"Video source {sourceId}";
        var temp = Path.Combine(Path.GetTempPath(), "TFlix", "Highlights", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var uploadedUrls = new List<string>();
        try
        {
            var duration = await ProbeDurationAsync(url, cancellationToken);
            if (duration < 20) return Fail("400", "Video quá ngắn để tạo highlight.");
            var frames = await ExtractFramesAsync(url, duration, temp, cancellationToken);
            var prompt = $"""
                Bạn là biên tập viên trailer. Các ảnh theo đúng thứ tự thời gian của video '{contentTitle}',
                thời lượng {duration:F1} giây. Hãy chọn đúng 3 đoạn nổi bật, khác nhau, mỗi đoạn 15-30 giây.
                Không chọn đoạn mở đầu/credit, không vượt quá thời lượng. Trả JSON array duy nhất gồm:
                title, description, startSeconds, endSeconds. Không markdown.
                """;
            var json = await _gemini.GenerateWithImagesAsync(prompt, frames, cancellationToken);
            var suggestions = ParseSuggestions(json, duration);
            if (suggestions.Count == 0) return Fail("502", "Gemini không xác định được đoạn highlight phù hợp.");

            var order = 0;
            foreach (var suggestion in suggestions.Take(3))
            {
                order++;
                var clip = Path.Combine(temp, $"highlight-{order}.mp4");
                var thumbnail = Path.Combine(temp, $"highlight-{order}.jpg");
                await CutClipAsync(url, suggestion.StartSeconds, suggestion.EndSeconds, clip, cancellationToken);
                await ExtractThumbnailAsync(clip, thumbnail, cancellationToken);
                var basePath = $"highlights/{sourceId}/{Guid.NewGuid():N}";
                var videoUrl = await UploadAsync(clip, $"{basePath}.mp4", "video/mp4", cancellationToken);
                var thumbnailUrl = await UploadAsync(thumbnail, $"{basePath}.jpg", "image/jpeg", cancellationToken);
                if (videoUrl == null) throw new InvalidOperationException("Không upload được clip highlight.");
                uploadedUrls.Add(videoUrl);
                if (thumbnailUrl != null) uploadedUrls.Add(thumbnailUrl);
                var add = await _highlights.AddAsync(new CreateVideoHighlightDto
                {
                    SourceId = sourceId, Title = suggestion.Title, Description = suggestion.Description,
                    StartSeconds = (decimal)suggestion.StartSeconds, EndSeconds = (decimal)suggestion.EndSeconds,
                    VideoUrl = videoUrl, ThumbnailUrl = thumbnailUrl, SortOrder = order
                });
                if (!add.Success) throw new InvalidOperationException(add.message);
            }
            return new CResponseMessage { Success = true, code = "200", message = $"Đã tạo {order} video highlight bằng Gemini." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Generate highlights failed for source {SourceId}", sourceId);
            foreach (var uploaded in uploadedUrls) await _r2.DeleteFileAsync(uploaded);
            return Fail("500", "Không tạo được highlight: " + ex.Message);
        }
        finally { try { Directory.Delete(temp, true); } catch { } }
    }

    private async Task<double> ProbeDurationAsync(string url, CancellationToken token)
    {
        var output = await RunAsync(_ffprobe, new[] { "-v", "error", "-show_entries", "format=duration", "-of", "default=noprint_wrappers=1:nokey=1", url }, token);
        return double.TryParse(output.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var duration) ? duration : 0;
    }

    private async Task<List<byte[]>> ExtractFramesAsync(string url, double duration, string temp, CancellationToken token)
    {
        var result = new List<byte[]>();
        for (var index = 1; index <= 8; index++)
        {
            var second = duration * index / 9d;
            var path = Path.Combine(temp, $"frame-{index}.jpg");
            await RunAsync(_ffmpeg, new[] { "-hide_banner", "-loglevel", "error", "-y", "-ss", second.ToString("F3", CultureInfo.InvariantCulture), "-i", url, "-frames:v", "1", "-vf", "scale=480:-2", path }, token);
            if (File.Exists(path)) result.Add(await File.ReadAllBytesAsync(path, token));
        }
        return result;
    }

    private Task CutClipAsync(string url, double start, double end, string output, CancellationToken token)
        => RunAsync(_ffmpeg, new[] { "-hide_banner", "-loglevel", "error", "-y", "-ss", start.ToString("F3", CultureInfo.InvariantCulture), "-i", url, "-t", (end-start).ToString("F3", CultureInfo.InvariantCulture), "-c:v", "libx264", "-preset", "veryfast", "-c:a", "aac", "-movflags", "+faststart", output }, token);
    private Task ExtractThumbnailAsync(string clip, string output, CancellationToken token)
        => RunAsync(_ffmpeg, new[] { "-hide_banner", "-loglevel", "error", "-y", "-ss", "1", "-i", clip, "-frames:v", "1", "-vf", "scale=640:-2", output }, token);

    private async Task<string?> UploadAsync(string path, string objectPath, string contentType, CancellationToken token)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var file = new FormFile(stream, 0, stream.Length, "file", Path.GetFileName(path)) { Headers = new HeaderDictionary(), ContentType = contentType };
        return await _r2.UploadFileAsync(file, objectPath);
    }

    private static List<GeminiHighlightSuggestion> ParseSuggestions(string? json, double duration)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        json = json.Replace("```json", "", StringComparison.OrdinalIgnoreCase).Replace("```", "").Trim();
        var items = JsonSerializer.Deserialize<List<GeminiHighlightSuggestion>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        return items.Where(x => x.StartSeconds >= 0 && x.EndSeconds > x.StartSeconds)
            .Select(x => { x.EndSeconds = Math.Min(duration, Math.Min(x.EndSeconds, x.StartSeconds + 30)); return x; })
            .Where(x => x.EndSeconds - x.StartSeconds >= 10).ToList();
    }

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
    {
        foreach (var name in names) if (row.Table.Columns.Contains(name) && row[name] != DBNull.Value) return row[name]?.ToString();
        return null;
    }
    private static CResponseMessage Fail(string code, string message) => new() { Success = false, code = code, message = message };
}
