using CoreLib.Dtos;
using CoreLib.Dtos.VideSoure;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Server.Forms;
using Server.Services;
using System.Collections.Concurrent;
using System.Text;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoSourcesController : ControllerBase
    {
        private const long MaxVideoUploadBytes = 1024L * 1024 * 1024;
        private const long MaxVideoChunkBytes = 8L * 1024 * 1024;
        private const int MaxVideoChunkCount = 100000;
        private const int MaxParallelSegmentUploads = 4;
        private static readonly string UploadRoot = Path.Combine(Path.GetTempPath(), "TFlix", "Mp4Uploads");
        private readonly ISupabaseService _supabase;
        private readonly ICloudflareR2Service _cloudflareR2;
        private readonly ICVideoSoure _videoSourceService;
        private readonly IVideoTranscodingService _videoTranscodingService;
        private readonly ILogger<VideoSourcesController> _logger;
        private readonly VideoUploadProgressTracker _uploadProgress;
        private readonly string _cloudflarePublicBaseUrl;
        private readonly string _supabaseBaseUrl;

        public VideoSourcesController(
            ISupabaseService supabase,
            ICloudflareR2Service cloudflareR2,
            ICVideoSoure videoSourceService,
            IVideoTranscodingService videoTranscodingService,
            ILogger<VideoSourcesController> logger,
            IConfiguration configuration,
            VideoUploadProgressTracker uploadProgress)
        {
            _supabase = supabase;
            _cloudflareR2 = cloudflareR2;
            _videoSourceService = videoSourceService;
            _videoTranscodingService = videoTranscodingService;
            _logger = logger;
            _uploadProgress = uploadProgress;
            _cloudflarePublicBaseUrl = (configuration["CloudflareR2:PublicBaseUrl"] ?? string.Empty).TrimEnd('/');
            _supabaseBaseUrl = (configuration["Supabase:Url"] ?? string.Empty).TrimEnd('/');
        }

        // =========================================================
        //  HLS: Playlist + Segments
        // =========================================================
        [HttpPost("add-hls")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxVideoUploadBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxVideoUploadBytes)]
        public async Task<IActionResult> UploadHlsAndCreate([FromForm] AddHlsVideoSourceForm form)
        {
            StoredVideoObject? uploadedPlaylist = null;
            decimal? sourceId = form.SourceId;
            var createdSource = false;
            var sourceUpdated = false;
            var uploadedObjects = new List<StoredVideoObject>();

            try
            {
                if (sourceId.HasValue && sourceId.Value <= 0)
                    return BadRequest("SourceId không hợp lệ.");

                // 1) Validate
                if (form.Playlist == null || form.Playlist.Length == 0)
                    return BadRequest("Playlist HLS (.m3u8) không được rỗng.");

                if (form.Segments == null || form.Segments.Count == 0)
                    return BadRequest("Danh sách segments không được rỗng.");

                var hasMovie = form.MovieId.HasValue;
                var hasEpisode = form.EpisodeId.HasValue;
                if (hasMovie == hasEpisode)
                    return BadRequest("Phải chọn duy nhất một đích: movie_id hoặc episode_id.");

                var status = (form.Status ?? "ACTIVE").Trim().ToUpperInvariant();

                // 2) Tạo basePath dùng chung cho playlist + segments
                var ownerId = form.MovieId ?? form.EpisodeId ?? 0;
                var quality = string.IsNullOrWhiteSpace(form.Quality) ? "unknown" : form.Quality.Trim();
                var basePath = $"hls/{ownerId}/{quality}/{Guid.NewGuid():N}/"; // ví dụ: hls/42/720p/xxxxxxxxxx/

                var segmentMap = new Dictionary<string, (StoredVideoObject Stored, long ByteSize)>(StringComparer.OrdinalIgnoreCase);
                foreach (var segment in form.Segments)
                {
                    var segmentName = Path.GetFileName(segment.FileName);
                    var storedSegment = await UploadVideoObjectAsync(segment, $"{basePath}{segmentName}");
                    if (storedSegment == null)
                        throw new InvalidOperationException($"Không upload được HLS segment {segmentName} lên Cloudflare hoặc Supabase.");

                    segmentMap[segmentName] = (storedSegment, segment.Length);
                    uploadedObjects.Add(storedSegment);
                }

                var playlistText = await ReadFormFileTextAsync(form.Playlist);
                var playlistLines = playlistText.Replace("\r\n", "\n").Split('\n');
                var orderedSegments = new List<(StoredVideoObject Stored, long ByteSize)>();
                for (var lineIndex = 0; lineIndex < playlistLines.Length; lineIndex++)
                {
                    var segmentReference = playlistLines[lineIndex].Trim();
                    if (string.IsNullOrWhiteSpace(segmentReference) || segmentReference.StartsWith('#'))
                        continue;

                    var referencePath = Uri.TryCreate(segmentReference, UriKind.Absolute, out var segmentUri)
                        ? segmentUri.AbsolutePath
                        : segmentReference.Split('?', '#')[0];
                    var segmentName = Uri.UnescapeDataString(Path.GetFileName(referencePath));
                    if (!segmentMap.TryGetValue(segmentName, out var storedSegment))
                        throw new InvalidOperationException($"Playlist tham chiếu segment chưa được upload: {segmentName}");

                    playlistLines[lineIndex] = storedSegment.Stored.Url;
                    orderedSegments.Add(storedSegment);
                }

                if (orderedSegments.Count == 0)
                    throw new InvalidOperationException("Playlist HLS không tham chiếu segment nào.");

                var rewrittenPlaylist = string.Join("\n", playlistLines);
                await using var playlistStream = new MemoryStream(Encoding.UTF8.GetBytes(rewrittenPlaylist));
                var playlistFile = new FormFile(playlistStream, 0, playlistStream.Length, "Playlist", "index.m3u8")
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "application/vnd.apple.mpegurl"
                };
                uploadedPlaylist = await UploadVideoObjectAsync(playlistFile, $"{basePath}index.m3u8");
                if (uploadedPlaylist == null)
                    throw new InvalidOperationException("Không upload được playlist HLS lên Cloudflare hoặc Supabase.");
                uploadedObjects.Add(uploadedPlaylist);

                // 4) Tạo video_sources
                var addDto = new AddVideoSourceDto
                {
                    MovieId = form.MovieId,
                    EpisodeId = form.EpisodeId,
                    Provider = GetStorageProviderName(uploadedObjects),
                    ServerName = form.ServerName,
                    StreamUrl = uploadedPlaylist.Url,
                    Quality = form.Quality,
                    Format = "HLS",
                    DrmType = form.DrmType,
                    DrmLicenseUrl = form.DrmLicenseUrl,
                    IsPrimary = form.IsPrimary,
                    Status = status
                };

                var sourceResp = sourceId.HasValue
                    ? await _videoSourceService.Update_video_source(new UpdateVideoSourceDto
                    {
                        SourceId = sourceId.Value,
                        Provider = addDto.Provider,
                        ServerName = addDto.ServerName,
                        StreamUrl = addDto.StreamUrl,
                        Quality = addDto.Quality,
                        Format = addDto.Format,
                        DrmType = addDto.DrmType,
                        DrmLicenseUrl = addDto.DrmLicenseUrl,
                        IsPrimary = addDto.IsPrimary,
                        Status = addDto.Status
                    })
                    : await _videoSourceService.Add_video_source(addDto);
                if (!sourceResp.Success)
                    throw new InvalidOperationException(sourceResp.message ?? "Không tạo được video source trong database.");

                if (sourceId.HasValue)
                {
                    sourceUpdated = true;
                }
                else
                {
                    if (sourceResp.Data == null)
                        throw new InvalidOperationException("Không lấy được dữ liệu từ Add_video_source.");

                    dynamic data = sourceResp.Data;
                    if (data.SourceId == null)
                        throw new InvalidOperationException("Không lấy được SourceId từ DB.");
                    sourceId = Convert.ToDecimal(data.SourceId);
                    createdSource = true;
                }

                var partDtos = orderedSegments.Select((segment, index) => new AddVideoSourcePartDto
                {
                    SourceId = sourceId.Value,
                    PartIndex = index,
                    Url = segment.Stored.Url,
                    ByteSize = segment.ByteSize
                }).ToList();
                var partsResponse = await _videoSourceService.Replace_video_source_parts(sourceId.Value, partDtos);
                if (!partsResponse.Success)
                    throw new InvalidOperationException(partsResponse.message ?? "Không cập nhật được HLS parts trong database.");

                if (sourceUpdated)
                    await DeleteReplacedStorageObjectsAsync(
                        form.OldStreamUrl,
                        partsResponse.Data as IEnumerable<string> ?? Enumerable.Empty<string>(),
                        uploadedObjects);

                return Ok(new
                {
                    sourceResp.code,
                    sourceResp.message,
                    sourceResp.Success,
                    playlistUrl = uploadedPlaylist.Url,
                    SourceId = sourceId.Value,
                    SegmentsCount = orderedSegments.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Manual HLS upload failed");
                if (!sourceUpdated)
                {
                    foreach (var storedObject in uploadedObjects)
                        await TryDeleteStorageObjectAsync(storedObject);
                }
                if (createdSource && sourceId.HasValue)
                    await _videoSourceService.Delete_video_source(sourceId.Value);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("mp4-uploads/{uploadId:guid}/chunks/{chunkIndex:int}")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(16L * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 16L * 1024 * 1024)]
        public async Task<IActionResult> UploadMp4Chunk(
            Guid uploadId,
            int chunkIndex,
            [FromForm(Name = "Chunk")] IFormFile? chunk,
            [FromForm] int totalChunks,
            [FromForm] string fileFingerprint,
            CancellationToken cancellationToken)
        {
            if (totalChunks < 1 || totalChunks > MaxVideoChunkCount || chunkIndex < 0 || chunkIndex >= totalChunks)
                return BadRequest(new { success = false, message = "Thông tin chunk không hợp lệ." });

            if (chunk == null || chunk.Length == 0 || chunk.Length > MaxVideoChunkBytes)
                return BadRequest(new { success = false, message = "Chunk rỗng hoặc vượt quá 8 MiB." });
            if (string.IsNullOrWhiteSpace(fileFingerprint) || fileFingerprint.Length > 512)
                return BadRequest(new { success = false, message = "Fingerprint file không hợp lệ." });

            var uploadDirectory = GetUploadDirectory(uploadId);
            if (!Directory.Exists(uploadDirectory))
                CleanupExpiredUploads();
            Directory.CreateDirectory(uploadDirectory);

            var manifestPath = Path.Combine(uploadDirectory, "chunk-count.txt");
            var fingerprintPath = Path.Combine(uploadDirectory, "file-fingerprint.txt");
            if (System.IO.File.Exists(manifestPath))
            {
                var savedChunkCount = await System.IO.File.ReadAllTextAsync(manifestPath, cancellationToken);
                if (!int.TryParse(savedChunkCount, out var parsedChunkCount) || parsedChunkCount != totalChunks)
                    return Conflict(new { success = false, message = "Số lượng chunk không khớp với upload đã bắt đầu." });

                if (System.IO.File.Exists(fingerprintPath))
                {
                    var savedFingerprint = await System.IO.File.ReadAllTextAsync(fingerprintPath, cancellationToken);
                    if (!string.Equals(savedFingerprint, fileFingerprint, StringComparison.Ordinal))
                        return Conflict(new { success = false, message = "File được chọn khác với phiên upload đã lưu." });
                }
                else
                {
                    await System.IO.File.WriteAllTextAsync(fingerprintPath, fileFingerprint, cancellationToken);
                }
            }
            else
            {
                await System.IO.File.WriteAllTextAsync(manifestPath, totalChunks.ToString(), cancellationToken);
                await System.IO.File.WriteAllTextAsync(fingerprintPath, fileFingerprint, cancellationToken);
            }

            var chunkPath = Path.Combine(uploadDirectory, $"chunk_{chunkIndex:D8}.part");
            await using (var target = new FileStream(chunkPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
            {
                await chunk.CopyToAsync(target, cancellationToken);
            }

            _uploadProgress.SetStage(uploadId, "receiving-chunks", "Đang nhận các phần MP4");

            _logger.LogInformation(
                "Stored MP4 upload chunk {ChunkIndex}/{TotalChunks} ({ChunkBytes} bytes) for upload {UploadId}",
                chunkIndex + 1,
                totalChunks,
                chunk.Length,
                uploadId);

            return Ok(new { success = true, chunkIndex, totalChunks });
        }

        [HttpGet("mp4-uploads/{uploadId:guid}/status")]
        public async Task<IActionResult> GetMp4UploadStatus(
            Guid uploadId,
            [FromQuery] string fileFingerprint,
            CancellationToken cancellationToken)
        {
            var uploadDirectory = GetUploadDirectory(uploadId);
            var manifestPath = Path.Combine(uploadDirectory, "chunk-count.txt");
            var fingerprintPath = Path.Combine(uploadDirectory, "file-fingerprint.txt");
            if (!System.IO.File.Exists(manifestPath) || !System.IO.File.Exists(fingerprintPath))
            {
                return Ok(new Mp4UploadStatusDto
                {
                    UploadId = uploadId,
                    ResumeAvailable = false
                });
            }

            var savedFingerprint = await System.IO.File.ReadAllTextAsync(fingerprintPath, cancellationToken);
            if (!string.Equals(savedFingerprint, fileFingerprint, StringComparison.Ordinal))
                return Conflict(new { success = false, message = "File được chọn khác với phiên upload đã lưu." });

            var chunkCountText = await System.IO.File.ReadAllTextAsync(manifestPath, cancellationToken);
            if (!int.TryParse(chunkCountText, out var totalChunks))
                return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = "Metadata upload bị lỗi." });

            var uploadedChunks = Directory.EnumerateFiles(uploadDirectory, "chunk_*.part")
                .Select(Path.GetFileNameWithoutExtension)
                .Select(name => int.TryParse(name.AsSpan("chunk_".Length), out var index) ? index : -1)
                .Where(index => index >= 0 && index < totalChunks)
                .OrderBy(index => index)
                .ToList();

            var progress = _uploadProgress.TryGetProgress(uploadId, out var trackedProgress)
                ? trackedProgress
                : new VideoUploadProgressDto(
                    "receiving-chunks",
                    $"Đã nhận {uploadedChunks.Count}/{totalChunks} phần MP4",
                    0,
                    0);

            return Ok(new Mp4UploadStatusDto
            {
                UploadId = uploadId,
                ResumeAvailable = true,
                TotalChunks = totalChunks,
                UploadedChunks = uploadedChunks,
                Progress = progress
            });
        }

        [Authorize]
        [HttpPost("mp4-uploads/{uploadId:guid}/complete")]
        public async Task<IActionResult> CompleteMp4Upload(
            Guid uploadId,
            [FromBody] CompleteMp4VideoUploadDto request,
            CancellationToken cancellationToken)
        {
            if (request.TotalChunks < 1 || request.TotalChunks > MaxVideoChunkCount)
                return BadRequest(new { success = false, message = "Số lượng chunk không hợp lệ." });

            if (!string.Equals(Path.GetExtension(request.FileName), ".mp4", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "Chỉ nhận file MP4." });

            if (request.MovieId.HasValue == request.EpisodeId.HasValue)
                return BadRequest(new { success = false, message = "Phải chọn duy nhất một Movie hoặc Episode." });
            if (request.SourceId.HasValue && request.SourceId.Value <= 0)
                return BadRequest(new { success = false, message = "SourceId không hợp lệ." });

            var uploadDirectory = GetUploadDirectory(uploadId);
            _uploadProgress.SetStage(uploadId, "packaging", "Đang đóng gói MP4 thành HLS");
            StoredVideoObject? uploadedPlaylist = null;
            decimal? sourceId = request.SourceId;
            var uploadedObjects = new ConcurrentBag<StoredVideoObject>();
            var uploadCompleted = false;
            var createdSource = false;

            try
            {
                var manifestPath = Path.Combine(uploadDirectory, "chunk-count.txt");
                if (!System.IO.File.Exists(manifestPath))
                    return NotFound(new { success = false, message = "Không tìm thấy dữ liệu upload tạm." });

                var savedChunkCount = await System.IO.File.ReadAllTextAsync(manifestPath, cancellationToken);
                if (!int.TryParse(savedChunkCount, out var parsedChunkCount) || parsedChunkCount != request.TotalChunks)
                    return Conflict(new { success = false, message = "Số lượng chunk không khớp." });

                var inputPath = Path.Combine(uploadDirectory, "source.mp4");
                var assembledLength = 0L;
                for (var index = 0; index < request.TotalChunks; index++)
                {
                    var chunkPath = Path.Combine(uploadDirectory, $"chunk_{index:D8}.part");
                    if (!System.IO.File.Exists(chunkPath))
                        return BadRequest(new { success = false, message = $"Thiếu chunk {index + 1}." });
                    assembledLength += new FileInfo(chunkPath).Length;
                }

                if (!System.IO.File.Exists(inputPath) || new FileInfo(inputPath).Length != assembledLength)
                {
                    var assemblingPath = inputPath + ".assembling";
                    await using (var output = new FileStream(assemblingPath, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, true))
                    {
                        for (var index = 0; index < request.TotalChunks; index++)
                        {
                            var chunkPath = Path.Combine(uploadDirectory, $"chunk_{index:D8}.part");
                            await using var chunk = new FileStream(chunkPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, true);
                            await chunk.CopyToAsync(output, cancellationToken);
                        }
                    }

                    System.IO.File.Move(assemblingPath, inputPath, true);
                }

                var hlsDirectory = Path.Combine(uploadDirectory, "hls");
                var playlistPath = Path.Combine(hlsDirectory, "index.m3u8");
                var existingSegments = Directory.Exists(hlsDirectory)
                    ? Directory.EnumerateFiles(hlsDirectory, "*.ts").Any()
                    : false;
                if (!System.IO.File.Exists(playlistPath) || !existingSegments)
                {
                    if (Directory.Exists(hlsDirectory))
                        Directory.Delete(hlsDirectory, true);
                    playlistPath = await _videoTranscodingService.CreateHlsAsync(inputPath, hlsDirectory, cancellationToken);
                }
                var ownerId = request.MovieId ?? request.EpisodeId!.Value;
                var quality = string.IsNullOrWhiteSpace(request.Quality) ? "unknown" : request.Quality.Trim();
                var basePathManifest = Path.Combine(uploadDirectory, "storage-basepath.txt");
                var basePath = System.IO.File.Exists(basePathManifest)
                    ? await System.IO.File.ReadAllTextAsync(basePathManifest, cancellationToken)
                    : $"hls/{ownerId}/{SanitizePathSegment(quality)}/{uploadId:N}/";
                if (!System.IO.File.Exists(basePathManifest))
                    await System.IO.File.WriteAllTextAsync(basePathManifest, basePath, cancellationToken);

                var segmentPaths = Directory.EnumerateFiles(hlsDirectory, "*.ts")
                    .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (segmentPaths.Count == 0)
                    throw new InvalidOperationException("FFmpeg không tạo được HLS segments.");

                var receiptDirectory = Path.Combine(uploadDirectory, "uploaded-objects");
                Directory.CreateDirectory(receiptDirectory);
                var segmentMap = new ConcurrentDictionary<string, (StoredVideoObject Stored, long ByteSize)>(StringComparer.OrdinalIgnoreCase);
                foreach (var segmentPath in segmentPaths)
                {
                    var segmentName = Path.GetFileName(segmentPath);
                    var receiptPath = GetSegmentReceiptPath(receiptDirectory, segmentName);
                    var receipt = await ReadStoredObjectReceiptAsync(receiptPath, cancellationToken);
                    if (receipt == null || receipt.ByteSize != new FileInfo(segmentPath).Length)
                        continue;

                    var storedObject = new StoredVideoObject(receipt.Url, receipt.IsCloudflare);
                    segmentMap[segmentName] = (storedObject, receipt.ByteSize);
                    uploadedObjects.Add(storedObject);
                }

                _uploadProgress.StartSegmentUpload(uploadId, segmentPaths.Count, segmentMap.Count);
                var pendingSegmentPaths = segmentPaths
                    .Where(path => !segmentMap.ContainsKey(Path.GetFileName(path)))
                    .ToList();
                await Parallel.ForEachAsync(
                    pendingSegmentPaths,
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = MaxParallelSegmentUploads,
                        CancellationToken = cancellationToken
                    },
                    async (segmentPath, token) =>
                    {
                        var segmentName = Path.GetFileName(segmentPath);
                        var storedSegment = await UploadLocalHlsFileAsync(segmentPath, $"{basePath}{segmentName}", "video/mp2t");
                        if (storedSegment == null)
                            throw new InvalidOperationException($"Không upload được HLS segment {segmentName} lên Cloudflare hoặc Supabase.");

                        var byteSize = new FileInfo(segmentPath).Length;
                        var receipt = new StoredObjectReceipt(storedSegment.Url, storedSegment.IsCloudflare, byteSize);
                        await WriteStoredObjectReceiptAsync(
                            GetSegmentReceiptPath(receiptDirectory, segmentName),
                            receipt,
                            token);
                        segmentMap[segmentName] = (storedSegment, byteSize);
                        uploadedObjects.Add(storedSegment);
                        var progress = _uploadProgress.IncrementUploadedSegment(uploadId);
                        _logger.LogInformation(
                            "Uploaded HLS segment {UploadedSegments}/{TotalSegments} for upload {UploadId}",
                            progress.SegmentsUploaded,
                            progress.TotalSegments,
                            uploadId);
                    });

                _uploadProgress.SetStage(uploadId, "saving", "Đã upload đủ segments; đang lưu playlist và nguồn video");

                var playlistLines = await System.IO.File.ReadAllLinesAsync(playlistPath, cancellationToken);
                for (var lineIndex = 0; lineIndex < playlistLines.Length; lineIndex++)
                {
                    var segmentReference = playlistLines[lineIndex].Trim();
                    if (string.IsNullOrWhiteSpace(segmentReference) || segmentReference.StartsWith('#'))
                        continue;

                    var referencePath = Uri.TryCreate(segmentReference, UriKind.Absolute, out var segmentUri)
                        ? segmentUri.AbsolutePath
                        : segmentReference.Split('?', '#')[0];
                    var segmentName = Uri.UnescapeDataString(Path.GetFileName(referencePath));
                    if (!segmentMap.TryGetValue(segmentName, out var storedSegment))
                        throw new InvalidOperationException($"Playlist tham chiếu segment không tồn tại: {segmentName}");

                    playlistLines[lineIndex] = storedSegment.Stored.Url;
                }

                var rewrittenPlaylistPath = Path.Combine(hlsDirectory, "index-upload.m3u8");
                await System.IO.File.WriteAllLinesAsync(rewrittenPlaylistPath, playlistLines, cancellationToken);
                var playlistReceiptPath = Path.Combine(receiptDirectory, "index.m3u8.json");
                var playlistReceipt = await ReadStoredObjectReceiptAsync(playlistReceiptPath, cancellationToken);
                if (playlistReceipt != null)
                {
                    uploadedPlaylist = new StoredVideoObject(playlistReceipt.Url, playlistReceipt.IsCloudflare);
                }
                else
                {
                    uploadedPlaylist = await UploadLocalHlsFileAsync(
                        rewrittenPlaylistPath,
                        $"{basePath}index.m3u8",
                        "application/vnd.apple.mpegurl");
                    if (uploadedPlaylist != null)
                    {
                        await WriteStoredObjectReceiptAsync(
                            playlistReceiptPath,
                            new StoredObjectReceipt(uploadedPlaylist.Url, uploadedPlaylist.IsCloudflare, new FileInfo(rewrittenPlaylistPath).Length),
                            cancellationToken);
                    }
                }
                if (uploadedPlaylist == null)
                    throw new InvalidOperationException("Không upload được playlist HLS lên Cloudflare hoặc Supabase.");
                uploadedObjects.Add(uploadedPlaylist);

                var sourceDto = new AddVideoSourceDto
                {
                    MovieId = request.MovieId,
                    EpisodeId = request.EpisodeId,
                    Provider = GetStorageProviderName(uploadedObjects),
                    ServerName = request.ServerName,
                    StreamUrl = uploadedPlaylist.Url,
                    Quality = request.Quality,
                    Format = "HLS",
                    DrmType = request.DrmType,
                    DrmLicenseUrl = request.DrmLicenseUrl,
                    IsPrimary = request.IsPrimary,
                    Status = (request.Status ?? "ACTIVE").Trim().ToUpperInvariant()
                };
                var sourceResponse = sourceId.HasValue
                    ? await _videoSourceService.Update_video_source(new UpdateVideoSourceDto
                    {
                        SourceId = sourceId.Value,
                        Provider = sourceDto.Provider,
                        ServerName = sourceDto.ServerName,
                        StreamUrl = sourceDto.StreamUrl,
                        Quality = sourceDto.Quality,
                        Format = sourceDto.Format,
                        DrmType = sourceDto.DrmType,
                        DrmLicenseUrl = sourceDto.DrmLicenseUrl,
                        IsPrimary = sourceDto.IsPrimary,
                        Status = sourceDto.Status
                    })
                    : await _videoSourceService.Add_video_source(sourceDto);

                if (!sourceResponse.Success || sourceResponse.Data == null)
                {
                    if (sourceId.HasValue && !sourceResponse.Success)
                        throw new InvalidOperationException(sourceResponse.message ?? "Không cập nhật được video source trong database.");
                    if (!sourceId.HasValue && !sourceResponse.Success)
                        throw new InvalidOperationException(sourceResponse.message ?? "Không tạo được video source trong database.");
                }

                if (sourceId.HasValue)
                {
                    _uploadProgress.SetStage(uploadId, "saving", "Đã cập nhật nguồn video; đang lưu thứ tự segments");
                }
                else
                {
                    dynamic sourceData = sourceResponse.Data!;
                    if (sourceData.SourceId == null)
                        throw new InvalidOperationException("Không lấy được SourceId từ database.");
                    sourceId = Convert.ToDecimal(sourceData.SourceId);
                    createdSource = true;
                }

                var partDtos = segmentPaths.Select((segmentPath, index) =>
                {
                    var segmentName = Path.GetFileName(segmentPath);
                    if (!segmentMap.TryGetValue(segmentName, out var segment))
                        throw new InvalidOperationException($"Không tìm thấy HLS segment đã upload: {segmentName}");

                    return new AddVideoSourcePartDto
                    {
                        SourceId = sourceId.Value,
                        PartIndex = index,
                        Url = segment.Stored.Url,
                        ByteSize = segment.ByteSize
                    };
                }).ToList();
                var partsResponse = await _videoSourceService.Replace_video_source_parts(sourceId.Value, partDtos);
                if (!partsResponse.Success)
                    throw new InvalidOperationException(partsResponse.message ?? "Không cập nhật được HLS parts trong database.");

                if (request.SourceId.HasValue)
                    await DeleteReplacedStorageObjectsAsync(
                        request.OldStreamUrl,
                        partsResponse.Data as IEnumerable<string> ?? Enumerable.Empty<string>(),
                        uploadedObjects);

                _logger.LogInformation(
                    "Completed MP4-to-HLS upload {UploadId} as source {SourceId} with {SegmentCount} segments",
                    uploadId,
                    sourceId,
                    segmentMap.Count);
                _uploadProgress.SetStage(uploadId, "complete", "Đã upload và lưu đủ HLS segments");
                uploadCompleted = true;

                return Ok(new
                {
                    sourceResponse.code,
                    sourceResponse.message,
                    sourceResponse.Success,
                    playlistUrl = uploadedPlaylist.Url,
                    SourceId = sourceId,
                    SegmentsCount = segmentMap.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MP4-to-HLS upload failed for upload {UploadId}", uploadId);
                _uploadProgress.SetStage(uploadId, "failed", ex.Message);
                if (createdSource && sourceId.HasValue)
                    await _videoSourceService.Delete_video_source(sourceId.Value);

                return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = ex.Message });
            }
            finally
            {
                if (uploadCompleted && Directory.Exists(uploadDirectory))
                {
                    try
                    {
                        Directory.Delete(uploadDirectory, true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not remove temporary MP4 upload {UploadId}", uploadId);
                    }
                }
            }
        }

        [HttpPost("mp4-uploads/{uploadId:guid}/cancel")]
        public async Task<IActionResult> CancelMp4Upload(Guid uploadId, CancellationToken cancellationToken)
        {
            var uploadDirectory = GetUploadDirectory(uploadId);
            var receiptDirectory = Path.Combine(uploadDirectory, "uploaded-objects");
            if (Directory.Exists(receiptDirectory))
            {
                foreach (var receiptPath in Directory.EnumerateFiles(receiptDirectory, "*.json"))
                {
                    var receipt = await ReadStoredObjectReceiptAsync(receiptPath, cancellationToken);
                    if (receipt != null)
                        await TryDeleteStorageObjectAsync(new StoredVideoObject(receipt.Url, receipt.IsCloudflare));
                }
            }

            if (Directory.Exists(uploadDirectory))
                Directory.Delete(uploadDirectory, true);
            _uploadProgress.Remove(uploadId);
            return Ok(new { success = true });
        }

        // =========================================================
        //  ADD video thường (1 file)
        // =========================================================
        [HttpPost("add")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxVideoUploadBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxVideoUploadBytes)]
        public async Task<IActionResult> UploadAndCreate([FromForm] AddVideoSourceForm form)
        {
            var hasFile = form.File != null && form.File.Length > 0;
            if (!hasFile && string.IsNullOrWhiteSpace(form.StreamUrl))
                return BadRequest("Cần chọn file video hoặc nhập Stream URL.");

            var hasMovie = form.MovieId.HasValue;
            var hasEpisode = form.EpisodeId.HasValue;
            if (hasMovie == hasEpisode)
                return BadRequest("Phải chọn duy nhất một đích: movie_id hoặc episode_id.");

            var status = (form.Status ?? "ACTIVE").Trim().ToUpperInvariant();

            // Đặt folder riêng cho video thường
            var ownerId = form.MovieId ?? form.EpisodeId ?? 0;
            StoredVideoObject? uploadedFile = null;
            if (hasFile)
            {
                var folder = $"videos/{ownerId}/{Guid.NewGuid():N}/";
                var objectPath = $"{folder}{form.File!.FileName}";
                uploadedFile = await UploadVideoObjectAsync(form.File!, objectPath);
                if (uploadedFile == null)
                    return StatusCode(500, new { success = false, message = "Upload video lên Cloudflare và Supabase đều thất bại." });
            }

            var dto = new AddVideoSourceDto
            {
                MovieId = form.MovieId,
                EpisodeId = form.EpisodeId,
                Provider = uploadedFile == null ? form.Provider ?? string.Empty : GetStorageProviderName(new[] { uploadedFile }),
                ServerName = form.ServerName,
                StreamUrl = uploadedFile?.Url ?? form.StreamUrl!.Trim(),
                Quality = form.Quality,
                Format = form.Format,
                DrmType = form.DrmType,
                DrmLicenseUrl = form.DrmLicenseUrl,
                IsPrimary = form.IsPrimary,
                Status = status
            };

            var resp = await _videoSourceService.Add_video_source(dto);

            if (!resp.Success)
            {
                if (uploadedFile != null)
                    await TryDeleteStorageObjectAsync(uploadedFile);
                return StatusCode(500, resp.message);
            }

            return Ok(new { resp.code, resp.message, resp.Success, publicUrl = uploadedFile?.Url, resp.Data });
        }

        // =========================================================
        //  GET
        // =========================================================
        [HttpGet("getall")]
        public IActionResult GetAll()
        {
            var result = _videoSourceService.get_all();
            return Ok(result);
        }

        [HttpGet("getbyid")]
        public IActionResult Getid(int id)
        {
            var result = _videoSourceService.get_bu_id(id);
            return Ok(result);
        }

        // =========================================================
        //  DELETE VIDEO SOURCE
        // =========================================================
        [HttpPost("delete")]
        [HttpDelete("{id:decimal}")]
        public async Task<IActionResult> DeleteSource([FromBody] System.Text.Json.JsonElement? body, [FromRoute] decimal? id, [FromQuery] string? streamUrl = null)
        {
            decimal sourceId = id ?? 0;
            string? effectiveStreamUrl = streamUrl;

            if (body.HasValue)
            {
                var el = body.Value;
                if (el.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    sourceId = el.GetDecimal();
                }
                else if (el.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    if (el.TryGetProperty("id", out var idProp) || el.TryGetProperty("Id", out idProp) || el.TryGetProperty("sourceId", out idProp) || el.TryGetProperty("SourceId", out idProp))
                    {
                        if (idProp.ValueKind == System.Text.Json.JsonValueKind.Number)
                            sourceId = idProp.GetDecimal();
                        else if (idProp.ValueKind == System.Text.Json.JsonValueKind.String && decimal.TryParse(idProp.GetString(), out var parsedId))
                            sourceId = parsedId;
                    }
                    if (string.IsNullOrWhiteSpace(effectiveStreamUrl) && (el.TryGetProperty("streamUrl", out var urlProp) || el.TryGetProperty("StreamUrl", out urlProp)))
                    {
                        effectiveStreamUrl = urlProp.GetString();
                    }
                }
            }

            if (sourceId <= 0)
                return BadRequest(new { code = "400", message = "Source ID không hợp lệ." });

            _logger.LogInformation("Deleting video source {SourceId}", sourceId);
            var response = await _videoSourceService.Delete_video_source(sourceId);
            if (response.code != "200" && !response.Success)
                return StatusCode(500, new { code = response.code, message = response.message });

            if (!string.IsNullOrWhiteSpace(effectiveStreamUrl))
            {
                _ = Task.Run(async () =>
                {
                    try { await DeletePreviousStorageObjectAsync(effectiveStreamUrl); } catch { }
                });
            }

            return Ok(new { code = "200", message = "Xóa video source thành công.", success = true });
        }

        // =========================================================
        //  REPLACE FILE
        // =========================================================
        [HttpPut("{sourceId:decimal}/replace-file")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxVideoUploadBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxVideoUploadBytes)]
        public async Task<IActionResult> ReplaceFile(
            decimal sourceId,
            [FromForm] ReplaceVideoSourceForm form)
        {
            if (sourceId <= 0) return BadRequest("sourceId không hợp lệ.");
            if (form.File == null || form.File.Length == 0) return BadRequest("File rỗng.");

            // folder mới cho file mới
            var ownerId = (decimal?)(form.MovieId) ?? (decimal?)(form.EpisodeId) ?? sourceId;
            var folder = $"videos/{ownerId}/{Guid.NewGuid():N}/";
            var objectPath = $"{folder}{form.File.FileName}";

            var uploadedFile = await UploadVideoObjectAsync(form.File, objectPath);
            if (uploadedFile == null)
                return StatusCode(500, new { success = false, message = "Upload video lên Cloudflare và Supabase đều thất bại." });

            var dto = new UpdateVideoSourceDto
            {
                SourceId = sourceId,
                MovieId = form.MovieId,
                EpisodeId = form.EpisodeId,
                Provider = GetStorageProviderName(new[] { uploadedFile }),
                ServerName = form.ServerName,
                StreamUrl = uploadedFile.Url,
                Quality = form.Quality,
                Format = form.Format,
                DrmType = form.DrmType,
                DrmLicenseUrl = form.DrmLicenseUrl,
                IsPrimary = form.IsPrimary,
                Status = form.Status
            };

            var resp = await _videoSourceService.Update_video_source(dto);
            if (!resp.Success)
            {
                await TryDeleteStorageObjectAsync(uploadedFile);
                return StatusCode(500, resp.message);
            }

            var oldPartsResponse = await _videoSourceService.Replace_video_source_parts(
                sourceId,
                Array.Empty<AddVideoSourcePartDto>());
            if (!oldPartsResponse.Success)
            {
                _logger.LogError(
                    "Updated video source {SourceId}, but could not clear old HLS parts: {Message}",
                    sourceId,
                    oldPartsResponse.message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "Nguồn đã cập nhật nhưng không xóa được danh sách HLS cũ: " + oldPartsResponse.message
                });
            }

            await DeleteReplacedStorageObjectsAsync(
                form.OldStreamUrl,
                oldPartsResponse.Data as IEnumerable<string> ?? Enumerable.Empty<string>(),
                new[] { uploadedFile });

            return Ok(new { resp.code, resp.message, resp.Success, newUrl = uploadedFile.Url });
        }

        private static string GetUploadDirectory(Guid uploadId)
        {
            return Path.Combine(UploadRoot, uploadId.ToString("N"));
        }

        private static string GetSegmentReceiptPath(string receiptDirectory, string segmentName)
        {
            return Path.Combine(receiptDirectory, $"{Path.GetFileName(segmentName)}.json");
        }

        private async Task<StoredObjectReceipt?> ReadStoredObjectReceiptAsync(string receiptPath, CancellationToken cancellationToken)
        {
            if (!System.IO.File.Exists(receiptPath))
                return null;

            try
            {
                var json = await System.IO.File.ReadAllTextAsync(receiptPath, cancellationToken);
                return System.Text.Json.JsonSerializer.Deserialize<StoredObjectReceipt>(json);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not read upload receipt {ReceiptName}", Path.GetFileName(receiptPath));
                return null;
            }
        }

        private static async Task WriteStoredObjectReceiptAsync(
            string receiptPath,
            StoredObjectReceipt receipt,
            CancellationToken cancellationToken)
        {
            var temporaryPath = receiptPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            var json = System.Text.Json.JsonSerializer.Serialize(receipt);
            await System.IO.File.WriteAllTextAsync(temporaryPath, json, cancellationToken);
            System.IO.File.Move(temporaryPath, receiptPath, true);
        }

        private static string SanitizePathSegment(string value)
        {
            var safeValue = new string(value.Where(character => char.IsLetterOrDigit(character) || character is '-' or '_').ToArray());
            return string.IsNullOrWhiteSpace(safeValue) ? "unknown" : safeValue;
        }

        private void CleanupExpiredUploads()
        {
            if (!Directory.Exists(UploadRoot))
                return;

            var expiredBefore = DateTime.UtcNow.AddHours(-24);
            foreach (var directory in Directory.EnumerateDirectories(UploadRoot))
            {
                try
                {
                    if (Directory.GetLastWriteTimeUtc(directory) < expiredBefore)
                        Directory.Delete(directory, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not clean expired video upload directory");
                }
            }
        }

        private async Task<StoredVideoObject?> UploadLocalHlsFileAsync(string filePath, string objectPath, string contentType)
        {
            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
            var file = new FormFile(stream, 0, stream.Length, "File", Path.GetFileName(filePath))
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };

            return await UploadVideoObjectAsync(file, objectPath);
        }

        private static async Task<string> ReadFormFileTextAsync(IFormFile file)
        {
            await using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return await reader.ReadToEndAsync();
        }

        private async Task<StoredVideoObject?> UploadVideoObjectAsync(IFormFile file, string objectPath)
        {
            try
            {
                var cloudflareUrl = await _cloudflareR2.UploadFileAsync(file, objectPath);
                if (!string.IsNullOrWhiteSpace(cloudflareUrl))
                {
                    _logger.LogInformation("Stored video object in Cloudflare R2: {ObjectName}", Path.GetFileName(objectPath));
                    return new StoredVideoObject(cloudflareUrl, IsCloudflare: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cloudflare R2 upload failed; trying Supabase fallback for {ObjectName}", Path.GetFileName(objectPath));
            }

            _logger.LogWarning("Cloudflare R2 unavailable for {ObjectName}; trying Supabase fallback", Path.GetFileName(objectPath));
            try
            {
                var supabaseUrl = await _supabase.UploadFileAsync(file, objectPath);
                if (!string.IsNullOrWhiteSpace(supabaseUrl))
                {
                    _logger.LogInformation("Stored video object in Supabase fallback: {ObjectName}", Path.GetFileName(objectPath));
                    return new StoredVideoObject(supabaseUrl, IsCloudflare: false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Supabase fallback upload failed for {ObjectName}", Path.GetFileName(objectPath));
            }

            return null;
        }

        private async Task TryDeleteStorageObjectAsync(StoredVideoObject storedObject)
        {
            try
            {
                if (storedObject.IsCloudflare)
                    await _cloudflareR2.DeleteFileAsync(storedObject.Url);
                else
                    await _supabase.DeleteFileAsync(storedObject.Url);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not remove uploaded video object after a failed upload");
            }
        }

        private async Task<bool> DeletePreviousStorageObjectAsync(string url)
        {
            if (IsCloudflareUrl(url))
                return await _cloudflareR2.DeleteFileAsync(url);
            if (IsSupabaseUrl(url))
                return await _supabase.DeleteFileAsync(url);

            _logger.LogWarning("Cannot delete replaced video object outside configured storage providers: {ObjectUrl}", url);
            return false;
        }

        private async Task DeleteReplacedStorageObjectsAsync(
            string? oldStreamUrl,
            IEnumerable<string> oldPartUrls,
            IEnumerable<StoredVideoObject> replacementObjects)
        {
            var replacementUrls = replacementObjects
                .Select(item => item.Url)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var oldUrls = oldPartUrls
                .Append(oldStreamUrl ?? string.Empty)
                .Where(url => !string.IsNullOrWhiteSpace(url) && !replacementUrls.Contains(url))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var oldUrl in oldUrls)
            {
                try
                {
                    if (!await DeletePreviousStorageObjectAsync(oldUrl))
                        _logger.LogWarning("Storage did not confirm deletion of replaced video object {ObjectUrl}", oldUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not delete replaced video object {ObjectUrl}", oldUrl);
                }
            }
        }

        private static string GetStorageProviderName(IEnumerable<StoredVideoObject> storedObjects)
        {
            var providers = storedObjects
                .Select(item => item.IsCloudflare ? "CLOUDFLARE_R2" : "SUPABASE")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return providers.Length switch
            {
                1 => providers[0],
                > 1 => "CLOUDFLARE_R2+SUPABASE",
                _ => "UNKNOWN"
            };
        }

        private bool IsCloudflareUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var candidate)
                && Uri.TryCreate(_cloudflarePublicBaseUrl, UriKind.Absolute, out var cloudflareBase)
                && string.Equals(candidate.Host, cloudflareBase.Host, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsSupabaseUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var candidate)
                && Uri.TryCreate(_supabaseBaseUrl, UriKind.Absolute, out var supabaseBase)
                && string.Equals(candidate.Host, supabaseBase.Host, StringComparison.OrdinalIgnoreCase);
        }

        private sealed record StoredVideoObject(string Url, bool IsCloudflare);
        private sealed record StoredObjectReceipt(string Url, bool IsCloudflare, long ByteSize);
    }

    // ================= FORM MODELS =================

    public class AddVideoSourceForm
    {
        public IFormFile? File { get; set; }
        public string? StreamUrl { get; set; }
        public decimal? MovieId { get; set; }
        public decimal? EpisodeId { get; set; }
        public string? Provider { get; set; }
        public string? ServerName { get; set; }
        public string? Quality { get; set; }
        public string? Format { get; set; }
        public string? DrmType { get; set; }
        public string? DrmLicenseUrl { get; set; }
        public bool IsPrimary { get; set; }
        public string? Status { get; set; }
    }

    public class ReplaceVideoSourceForm
    {
        public IFormFile File { get; set; } = default!;

        public decimal? MovieId { get; set; }
        public decimal? EpisodeId { get; set; }
        public string? Provider { get; set; }
        public string? ServerName { get; set; }
        public string? Quality { get; set; }
        public string? Format { get; set; }
        public string? DrmType { get; set; }
        public string? DrmLicenseUrl { get; set; }
        public bool IsPrimary { get; set; }
        public string? Status { get; set; }

        public string? OldStreamUrl { get; set; }
    }
}
