using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
using CoreLib.Dtos.Subtitle;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Server.Services.Subtitles;

public sealed class BrowserSubtitleJobService : IBrowserSubtitleJobService
{
    private const int MaxChunks = 500;
    private const long MaxChunkBytes = 12L * 1024 * 1024;
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "TFlix", "BrowserSubtitleJobs");
    private static readonly SemaphoreSlim CompletionGate = new(1, 1);
    private readonly ICVideoSoure _sources;
    private readonly ICSubtitle _subtitles;
    private readonly IGeminiSubtitleService _gemini;
    private readonly ICloudflareR2Service _r2;
    private readonly IHttpClientFactory _httpClientFactory;

    public BrowserSubtitleJobService(ICVideoSoure sources, ICSubtitle subtitles,
        IGeminiSubtitleService gemini, ICloudflareR2Service r2, IHttpClientFactory httpClientFactory)
    {
        _sources = sources;
        _subtitles = subtitles;
        _gemini = gemini;
        _r2 = r2;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<(byte[] Content, string ContentType)> DownloadHlsFileAsync(long sourceId, string fileUrl,
        CancellationToken cancellationToken)
    {
        while (fileUrl.Contains("ProxyHlsFile", StringComparison.OrdinalIgnoreCase) && fileUrl.Contains("url=", StringComparison.OrdinalIgnoreCase))
        {
            var match = System.Text.RegularExpressions.Regex.Match(fileUrl, @"[?&]url=([^&]+)");
            if (match.Success)
            {
                fileUrl = Uri.UnescapeDataString(match.Groups[1].Value);
            }
            else break;
        }

        var sourceUri = new Uri(GetSourceUrl(sourceId), UriKind.Absolute);
        if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var requestedUri)
            || requestedUri.Scheme != Uri.UriSchemeHttps
            || !string.Equals(sourceUri.Host, requestedUri.Host, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("URL HLS không thuộc nguồn video đã lưu.");

        var sourceDirectory = sourceUri.AbsolutePath[..(sourceUri.AbsolutePath.LastIndexOf('/') + 1)];
        if (!requestedUri.AbsolutePath.StartsWith(sourceDirectory, StringComparison.Ordinal))
            throw new InvalidOperationException("File HLS nằm ngoài thư mục của nguồn video.");

        using var request = new HttpRequestMessage(HttpMethod.Get, requestedUri);
        using var response = await _httpClientFactory.CreateClient().SendAsync(request,
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Không tải được file HLS từ storage ({(int)response.StatusCode}).");
        var contentLength = response.Content.Headers.ContentLength;
        if (contentLength > 32L * 1024 * 1024)
            throw new InvalidOperationException("File HLS vượt quá giới hạn 32 MiB.");
        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (content.Length > 32L * 1024 * 1024)
            throw new InvalidOperationException("File HLS vượt quá giới hạn 32 MiB.");
        return (content, response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream");
    }

    public async Task<BrowserSubtitleJobDto> StartAsync(BrowserSubtitleJobRequestDto request, string ownerId,
        CancellationToken cancellationToken)
    {
        if (request.SourceId <= 0 || request.TotalChunks is < 1 or > MaxChunks)
            throw new InvalidOperationException("Thông tin tác vụ phụ đề không hợp lệ.");
        var streamUrl = GetSourceUrl(request.SourceId);
        ValidateHlsUrl(streamUrl);

        Directory.CreateDirectory(Root);
        var existing = Directory.EnumerateDirectories(Root)
            .Select(path => ReadManifestAsync(path, cancellationToken).GetAwaiter().GetResult())
            .FirstOrDefault(job => job != null && job.OwnerId == ownerId && job.SourceId == request.SourceId
                && job.Status != "Completed" && job.TotalChunks == request.TotalChunks);
        if (existing != null && !request.ForceRegenerate) return ToDto(existing);

        var manifest = new JobManifest
        {
            JobId = Guid.NewGuid(), SourceId = request.SourceId, OwnerId = ownerId,
            TotalChunks = request.TotalChunks, Status = "Extracting", StreamUrl = streamUrl
        };
        var directory = GetDirectory(manifest.JobId);
        Directory.CreateDirectory(directory);
        await SaveManifestAsync(directory, manifest, cancellationToken);
        return ToDto(manifest);
    }

    public async Task<BrowserSubtitleJobDto?> GetAsync(Guid jobId, string ownerId, CancellationToken cancellationToken)
    {
        var manifest = await RequireOwnedJobAsync(jobId, ownerId, cancellationToken, false);
        return manifest == null ? null : ToDto(manifest);
    }

    public async Task<BrowserSubtitleChunkResultDto> ProcessChunkAsync(Guid jobId, int chunkIndex,
        double offsetSeconds, double durationSeconds, IFormFile audio, string ownerId, CancellationToken cancellationToken)
    {
        var manifest = await RequireOwnedJobAsync(jobId, ownerId, cancellationToken, true)
            ?? throw new InvalidOperationException("Không tìm thấy tác vụ phụ đề.");
        if (chunkIndex < 0 || chunkIndex >= manifest.TotalChunks || offsetSeconds < 0 || durationSeconds is <= 0 or > 190)
            throw new InvalidOperationException("Metadata audio chunk không hợp lệ.");
        if (audio.Length <= 0 || audio.Length > MaxChunkBytes)
            throw new InvalidOperationException("Audio chunk rỗng hoặc vượt quá 12 MiB.");
        var resultPath = Path.Combine(GetDirectory(jobId), $"chunk-{chunkIndex:D4}.json");
        if (File.Exists(resultPath))
        {
            var saved = JsonSerializer.Deserialize<List<GeminiSourceSubtitleCue>>(
                await File.ReadAllTextAsync(resultPath, cancellationToken)) ?? new();
            return new BrowserSubtitleChunkResultDto { Success = true, Message = "Chunk đã xử lý.", ChunkIndex = chunkIndex, CueCount = saved.Count };
        }

        manifest.Status = "Transcribing";
        await SaveManifestAsync(GetDirectory(jobId), manifest, cancellationToken);
        await using var input = audio.OpenReadStream();
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, cancellationToken);
        var cues = await _gemini.TranscribeVietnameseAsync(buffer.ToArray(), audio.ContentType,
            offsetSeconds, durationSeconds, cancellationToken);
        await File.WriteAllTextAsync(resultPath, JsonSerializer.Serialize(cues), new UTF8Encoding(false), cancellationToken);
        manifest.CompletedChunks = Directory.EnumerateFiles(GetDirectory(jobId), "chunk-*.json").Count();
        manifest.Status = manifest.CompletedChunks == manifest.TotalChunks ? "Uploading" : "Extracting";
        await SaveManifestAsync(GetDirectory(jobId), manifest, cancellationToken);
        return new BrowserSubtitleChunkResultDto { Success = true, Message = "Đã dịch audio chunk.", ChunkIndex = chunkIndex, CueCount = cues.Count };
    }

    public async Task<BrowserSubtitleJobDto> CompleteAsync(Guid jobId, string ownerId, CancellationToken cancellationToken)
    {
        await CompletionGate.WaitAsync(cancellationToken);
        try
        {
            var manifest = await RequireOwnedJobAsync(jobId, ownerId, cancellationToken, true)
                ?? throw new InvalidOperationException("Không tìm thấy tác vụ phụ đề.");
            var files = Directory.EnumerateFiles(GetDirectory(jobId), "chunk-*.json").OrderBy(path => path).ToList();
            if (files.Count != manifest.TotalChunks)
                throw new InvalidOperationException($"Mới hoàn tất {files.Count}/{manifest.TotalChunks} audio chunk.");
            var cues = new List<GeminiSourceSubtitleCue>();
            foreach (var chunkFile in files)
                cues.AddRange(JsonSerializer.Deserialize<List<GeminiSourceSubtitleCue>>(
                    await File.ReadAllTextAsync(chunkFile, cancellationToken)) ?? new());
            cues = NormalizeCues(cues);
            if (cues.Count == 0) throw new InvalidOperationException("Không có lời thoại hợp lệ để tạo WebVTT.");

            manifest.Status = "PendingApproval";
            await SaveManifestAsync(GetDirectory(jobId), manifest, cancellationToken);
            await File.WriteAllTextAsync(GetDraftPath(jobId), JsonSerializer.Serialize(cues), new UTF8Encoding(false), cancellationToken);
            manifest.Message = "Đã tạo bản nháp phụ đề. Vui lòng xem trước, chỉnh sửa và duyệt.";
            await SaveManifestAsync(GetDirectory(jobId), manifest, cancellationToken);
            return ToDto(manifest);
        }
        catch (Exception exception)
        {
            var manifest = await ReadManifestAsync(GetDirectory(jobId), cancellationToken);
            if (manifest != null)
            {
                manifest.Status = "Failed";
                manifest.Message = exception.Message;
                await SaveManifestAsync(GetDirectory(jobId), manifest, cancellationToken);
            }
            throw;
        }
        finally { CompletionGate.Release(); }
    }

    public async Task<SubtitleDraftDto> GetDraftAsync(Guid jobId, string ownerId, CancellationToken cancellationToken)
    {
        var manifest = await RequireOwnedJobAsync(jobId, ownerId, cancellationToken, true)
            ?? throw new InvalidOperationException("Không tìm thấy tác vụ phụ đề.");
        var cues = await ReadDraftAsync(jobId, cancellationToken);
        return new SubtitleDraftDto { JobId = jobId, SourceId = manifest.SourceId, Status = manifest.Status,
            StreamUrl = manifest.StreamUrl, Cues = cues };
    }

    public async Task<SubtitleDraftDto> UpdateDraftAsync(Guid jobId, UpdateSubtitleDraftDto request, string ownerId,
        CancellationToken cancellationToken)
    {
        var manifest = await RequireOwnedJobAsync(jobId, ownerId, cancellationToken, true)
            ?? throw new InvalidOperationException("Không tìm thấy tác vụ phụ đề.");
        if (manifest.Status == "Completed") throw new InvalidOperationException("Phụ đề đã được duyệt.");
        var cues = NormalizeCues(request.Cues);
        if (cues.Count == 0) throw new InvalidOperationException("Bản nháp phải có ít nhất một đoạn phụ đề hợp lệ.");
        await File.WriteAllTextAsync(GetDraftPath(jobId), JsonSerializer.Serialize(cues), new UTF8Encoding(false), cancellationToken);
        manifest.Status = "PendingApproval";
        manifest.Message = "Đã lưu nội dung chỉnh sửa. Phụ đề chưa được duyệt.";
        await SaveManifestAsync(GetDirectory(jobId), manifest, cancellationToken);
        return new SubtitleDraftDto { JobId = jobId, SourceId = manifest.SourceId, Status = manifest.Status,
            StreamUrl = manifest.StreamUrl, Cues = cues };
    }

    public async Task<BrowserSubtitleJobDto> ApproveAsync(Guid jobId, string ownerId, CancellationToken cancellationToken)
    {
        await CompletionGate.WaitAsync(cancellationToken);
        try
        {
            var manifest = await RequireOwnedJobAsync(jobId, ownerId, cancellationToken, true)
                ?? throw new InvalidOperationException("Không tìm thấy tác vụ phụ đề.");
            var cues = await ReadDraftAsync(jobId, cancellationToken);
            if (cues.Count == 0) throw new InvalidOperationException("Không có bản nháp để duyệt.");
            manifest.Status = "Uploading";
            await SaveManifestAsync(GetDirectory(jobId), manifest, cancellationToken);
            var viUrl = await UploadVttAsync(manifest, cues, "vi", true, cancellationToken);
            await UploadVttAsync(manifest, cues, "en", false, cancellationToken);
            manifest.Status = "Completed";
            manifest.SubtitleUrl = viUrl;
            manifest.Message = "Đã duyệt và xuất bản phụ đề Tiếng Việt và Tiếng Anh.";
            await SaveManifestAsync(GetDirectory(jobId), manifest, cancellationToken);
            return ToDto(manifest);
        }
        finally { CompletionGate.Release(); }
    }

    public async Task<bool> SaveSourceSubtitlesAsync(long sourceId, List<GeminiSourceSubtitleCue> cues, CancellationToken cancellationToken)
    {
        await CompletionGate.WaitAsync(cancellationToken);
        try
        {
            var streamUrl = GetSourceUrl(sourceId);
            var normalized = NormalizeCues(cues);
            if (normalized.Count == 0) throw new InvalidOperationException("Danh sách phụ đề trống hoặc không hợp lệ.");

            var manifest = new JobManifest
            {
                SourceId = sourceId,
                StreamUrl = streamUrl
            };

            await UploadVttAsync(manifest, normalized, "vi", true, cancellationToken);
            await UploadVttAsync(manifest, normalized, "en", false, cancellationToken);
            return true;
        }
        finally
        {
            CompletionGate.Release();
        }
    }


    private static async Task<List<GeminiSourceSubtitleCue>> ReadDraftAsync(Guid jobId, CancellationToken token)
    {
        var path = GetDraftPath(jobId);
        if (!File.Exists(path)) throw new InvalidOperationException("Bản nháp phụ đề chưa được tạo.");
        return JsonSerializer.Deserialize<List<GeminiSourceSubtitleCue>>(await File.ReadAllTextAsync(path, token)) ?? new();
    }

    private string GetSourceUrl(long sourceId)
    {
        var response = _sources.GetBySourceId(sourceId);
        if (!response.Success || response.Data is not DataSet data || data.Tables.Count == 0 || data.Tables[0].Rows.Count == 0)
            throw new InvalidOperationException("Không tìm thấy VideoSource.");
        var row = data.Tables[0].Rows[0];
        foreach (var name in new[] { "stream_url", "StreamUrl" })
            if (row.Table.Columns.Contains(name) && row[name] != DBNull.Value) return row[name].ToString()!;
        throw new InvalidOperationException("VideoSource không có StreamUrl.");
    }

    private static void ValidateHlsUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps
            || !uri.AbsolutePath.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase)
            || uri.IsLoopback || uri.HostNameType == UriHostNameType.IPv4 || uri.HostNameType == UriHostNameType.IPv6)
            throw new InvalidOperationException("Chỉ hỗ trợ HLS HTTPS công khai, không chấp nhận địa chỉ nội bộ hoặc IP trực tiếp.");
    }

    private static List<GeminiSourceSubtitleCue> NormalizeCues(IEnumerable<GeminiSourceSubtitleCue> input)
    {
        var result = new List<GeminiSourceSubtitleCue>();
        double previousEnd = 0;
        foreach (var cue in input.OrderBy(item => item.StartSeconds))
        {
            var start = Math.Max(cue.StartSeconds, previousEnd);
            if (cue.EndSeconds <= start || string.IsNullOrWhiteSpace(cue.Vi) || string.IsNullOrWhiteSpace(cue.En)) continue;
            result.Add(new GeminiSourceSubtitleCue
                { StartSeconds = start, EndSeconds = cue.EndSeconds, Vi = cue.Vi.Trim(), En = cue.En.Trim() });
            previousEnd = cue.EndSeconds;
        }
        return result;
    }

    private async Task<string> UploadVttAsync(JobManifest manifest, List<GeminiSourceSubtitleCue> cues,
        string languageCode, bool isDefault, CancellationToken cancellationToken)
    {
        var bytes = new UTF8Encoding(false).GetBytes(BuildVtt(cues, languageCode));
        await using var stream = new MemoryStream(bytes);
        var file = new FormFile(stream, 0, bytes.Length, "file", $"{languageCode}.vtt")
            { Headers = new HeaderDictionary(), ContentType = "text/vtt" };
        var url = await _r2.UploadFileAsync(file, GetSubtitleObjectPath(manifest.StreamUrl, manifest.SourceId, languageCode))
            ?? throw new InvalidOperationException($"Không upload được phụ đề {languageCode.ToUpperInvariant()} lên Cloudflare R2.");
        var saved = await _subtitles.UpsertAsync(new CreateSubtitleDto
            { SourceId = manifest.SourceId, LanguageCode = languageCode, Format = "VTT", Url = url, IsDefault = isDefault });
        if (!saved.Success) throw new InvalidOperationException(saved.message);
        return url;
    }

    private static string BuildVtt(IEnumerable<GeminiSourceSubtitleCue> cues, string languageCode)
    {
        var output = new StringBuilder("WEBVTT\n\n");
        var index = 0;
        foreach (var cue in cues)
        {
            output.AppendLine((++index).ToString(CultureInfo.InvariantCulture));
            output.Append(FormatTime(cue.StartSeconds)).Append(" --> ").AppendLine(FormatTime(cue.EndSeconds));
            output.AppendLine(languageCode == "en" ? cue.En : cue.Vi).AppendLine();
        }
        return output.ToString();
    }

    private static string FormatTime(double seconds) =>
        TimeSpan.FromSeconds(Math.Max(0, seconds)).ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture);

    private static string GetSubtitleObjectPath(string streamUrl, long sourceId, string languageCode)
    {
        var path = Uri.UnescapeDataString(new Uri(streamUrl).AbsolutePath).TrimStart('/');
        var slash = path.LastIndexOf('/');
        return slash > 0 ? $"{path[..slash]}/subtitles/{languageCode}.vtt" : $"subtitles/{sourceId}/{languageCode}.vtt";
    }

    private static string GetDirectory(Guid jobId) => Path.Combine(Root, jobId.ToString("N"));
    private static string GetDraftPath(Guid jobId) => Path.Combine(GetDirectory(jobId), "draft.json");
    private static string GetManifestPath(string directory) => Path.Combine(directory, "manifest.json");
    private static async Task SaveManifestAsync(string directory, JobManifest value, CancellationToken token) =>
        await File.WriteAllTextAsync(GetManifestPath(directory), JsonSerializer.Serialize(value), new UTF8Encoding(false), token);
    private static async Task<JobManifest?> ReadManifestAsync(string directory, CancellationToken token) =>
        File.Exists(GetManifestPath(directory))
            ? JsonSerializer.Deserialize<JobManifest>(await File.ReadAllTextAsync(GetManifestPath(directory), token)) : null;
    private static async Task<JobManifest?> RequireOwnedJobAsync(Guid jobId, string ownerId, CancellationToken token, bool required)
    {
        var manifest = await ReadManifestAsync(GetDirectory(jobId), token);
        if (manifest == null) { if (required) throw new InvalidOperationException("Không tìm thấy tác vụ phụ đề."); return null; }
        if (!string.Equals(manifest.OwnerId, ownerId, StringComparison.Ordinal)) throw new UnauthorizedAccessException();
        return manifest;
    }
    private static BrowserSubtitleJobDto ToDto(JobManifest value) => new()
    {
        JobId = value.JobId, SourceId = value.SourceId, Status = value.Status, TotalChunks = value.TotalChunks,
        CompletedChunks = Directory.Exists(GetDirectory(value.JobId))
            ? Directory.EnumerateFiles(GetDirectory(value.JobId), "chunk-*.json")
                .Select(path => int.Parse(Path.GetFileNameWithoutExtension(path)[6..], CultureInfo.InvariantCulture)).ToList() : new(),
        Message = value.Message, SubtitleUrl = value.SubtitleUrl
    };
    private sealed class JobManifest
    {
        public Guid JobId { get; set; }
        public long SourceId { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public int TotalChunks { get; set; }
        public int CompletedChunks { get; set; }
        public string Status { get; set; } = "Pending";
        public string StreamUrl { get; set; } = string.Empty;
        public string? Message { get; set; }
        public string? SubtitleUrl { get; set; }
    }
}
