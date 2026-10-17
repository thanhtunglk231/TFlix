using CoreLib.Dtos.VideSoure;
using CoreLib.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Areas.Admin.Controllers
{

    public class VideoSoureController : AdminBaseController
    {
        private const long MaxVideoUploadBytes = 1024L * 1024 * 1024;
        private readonly IVideoSoureService _videoSoureService;
        private readonly ILogger<VideoSoureController> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private static readonly ConcurrentDictionary<Guid, BrowserHlsCompletionJob> BrowserHlsCompletionJobs = new();

        public VideoSoureController(IVideoSoureService videoSoureService, ILogger<VideoSoureController> logger, IServiceScopeFactory scopeFactory)
        {
            _videoSoureService = videoSoureService;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        private sealed class BrowserHlsCompletionJob
        {
            public long? AdminUserId { get; init; }
            public string Status { get; set; } = "PROCESSING";
            public CResponseMessage? Result { get; set; }
            public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClientLog([FromBody] VideoSourceClientLogDto? dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Event))
                return BadRequest(new { success = false, message = "Nội dung log không hợp lệ." });

            _logger.LogError(
                "[Admin/VideoSoure/Client] {Event} | Detail={Detail} | Page={PageUrl} | UserAgent={UserAgent} | AdminUserId={AdminUserId}",
                dto.Event[..Math.Min(dto.Event.Length, 200)],
                dto.Detail?[..Math.Min(dto.Detail.Length, 4000)],
                dto.PageUrl?[..Math.Min(dto.PageUrl.Length, 1000)],
                dto.UserAgent?[..Math.Min(dto.UserAgent.Length, 1000)],
                GetCurrentAdminUserId());

            return Ok(new { success = true });
        }

        public sealed class VideoSourceClientLogDto
        {
            public string Event { get; set; } = string.Empty;
            public string? Detail { get; set; }
            public string? PageUrl { get; set; }
            public string? UserAgent { get; set; }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MaxVideoUploadBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxVideoUploadBytes)]
        public async Task<IActionResult> Add(
            [FromForm(Name = "File")] IFormFile? file,
      [FromForm] AddVideoSourceInputDto addVideoSourceInputDto // bind từ form-data
  )
        {
            Console.WriteLine("[VideoSoureController] -> Add ENTER");

            // Dump toàn bộ key/value đã post lên để debug nhanh
            if (Request.HasFormContentType)
            {
                Console.WriteLine("[VideoSoureController] Posted form fields:");
                foreach (var k in Request.Form.Keys)
                {
                    // Tránh log raw file content: chỉ log tên file
                    if (string.Equals(k, "File", StringComparison.OrdinalIgnoreCase))
                        Console.WriteLine($"  {k} = (IFormFile) {file?.FileName} size={file?.Length}");
                    else
                        Console.WriteLine($"  {k} = {Request.Form[k]}");
                }
            }
            else
            {
                Console.WriteLine("[VideoSoureController] Warning: ContentType không phải form.");
            }

         

            var result = await _videoSoureService.add_VideoSoure(file, addVideoSourceInputDto);
            Console.WriteLine("[VideoSoureController] <- Add EXIT: " + JsonConvert.SerializeObject(result));
            return Ok(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MaxVideoUploadBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxVideoUploadBytes)]
        public async Task<IActionResult> AddHls(
            [FromForm(Name = "Playlist")] IFormFile? playlist,
            [FromForm(Name = "Segments")] List<IFormFile>? segments,
            [FromForm] AddVideoSourceInputDto addVideoSourceInputDto,
            [FromForm] decimal? sourceId,
            [FromForm] string? oldStreamUrl)
        {
            if (playlist == null || playlist.Length == 0 || segments == null || segments.Count == 0)
                return BadRequest(new { success = false, message = "Cần chọn playlist và ít nhất một segment HLS." });

            var result = await _videoSoureService.add_HlsVideoSource(
                playlist,
                segments,
                addVideoSourceInputDto,
                sourceId,
                oldStreamUrl);
            return Ok(result);
        }

        [HttpPut]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10L * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10L * 1024 * 1024)]
        public async Task<IActionResult> UploadBrowserHlsChunk(
            Guid uploadId,
            Guid fileId,
            int chunkIndex,
            [FromForm] string fileName,
            [FromForm] long fileSize,
            [FromForm] int totalChunks,
            [FromForm] string checksum,
            [FromForm(Name = "Chunk")] IFormFile? chunk)
        {
            if (uploadId == Guid.Empty || fileId == Guid.Empty || chunkIndex < 0 || chunk == null)
                return BadRequest(new { success = false, message = "Thông tin HLS chunk không hợp lệ." });

            var result = await _videoSoureService.UploadBrowserHlsChunkAsync(
                uploadId, fileId, fileName, fileSize, chunkIndex, totalChunks, checksum, chunk);
            return result.Success ? Ok(result) : StatusCode(500, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetBrowserHlsUploadStatus(Guid uploadId)
        {
            if (uploadId == Guid.Empty)
                return BadRequest(new { success = false, message = "UploadId không hợp lệ." });
            return Ok(await _videoSoureService.GetBrowserHlsUploadStatusAsync(uploadId));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteBrowserHlsUpload(Guid uploadId, [FromBody] CompleteBrowserHlsUploadDto request)
        {
            if (uploadId == Guid.Empty || request == null)
                return BadRequest(new { success = false, message = "Thông tin hoàn tất HLS không hợp lệ." });

            var adminUserId = GetCurrentAdminUserId();
            var adminToken = HttpContext.Session.GetString("AdminJWToken");
            if (string.IsNullOrWhiteSpace(adminToken))
                return Unauthorized(new CResponseMessage { Success = false, code = "401", message = "Phiên đăng nhập quản trị đã hết hạn." });
            var job = new BrowserHlsCompletionJob { AdminUserId = adminUserId };
            if (!BrowserHlsCompletionJobs.TryAdd(uploadId, job))
                return Accepted(BrowserHlsCompletionJobs[uploadId].Result ?? new CResponseMessage
                {
                    Success = true,
                    code = "202",
                    message = "HLS đang được hoàn tất trên server."
                });

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IVideoSoureService>();
                    try
                    {
                        job.Result = await service.CompleteBrowserHlsUploadAsync(uploadId, request, adminToken);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogWarning(exception, "HLS completion response was interrupted for {UploadId}; switching to status polling", uploadId);
                    }
                    if (job.Result == null)
                        job.Result = await ResolveEmptyCompletionResponseAsync(service, uploadId, adminToken);
                    job.Status = job.Result.Success ? "COMPLETED" : "FAILED";
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Background HLS completion failed for {UploadId}", uploadId);
                    job.Result = new CResponseMessage { Success = false, code = "500", message = "Lỗi hoàn tất HLS: " + exception.Message };
                    job.Status = "FAILED";
                }
            });

            return Accepted(new CResponseMessage
            {
                Success = true,
                code = "202",
                message = "Server đang ghép và lưu HLS."
            });
        }

        private static async Task<CResponseMessage> ResolveEmptyCompletionResponseAsync(IVideoSoureService service, Guid uploadId, string bearerToken)
        {
            for (var attempt = 0; attempt < 360; attempt++)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                BrowserHlsUploadStatusDto? status;
                try
                {
                    status = await service.GetBrowserHlsUploadStatusAsync(uploadId, bearerToken);
                }
                catch
                {
                    continue;
                }
                if (status == null)
                    continue;
                if (!status.Exists)
                    return new CResponseMessage
                    {
                        Success = true,
                        code = "200",
                        message = "Đã hoàn tất và lưu nguồn HLS."
                    };
                if (!status.IsCompleting)
                    return new CResponseMessage
                    {
                        Success = false,
                        code = "500",
                        message = "Server dừng hoàn tất HLS nhưng không trả về kết quả. Dữ liệu tạm được giữ lại để thử lại."
                    };
            }

            return new CResponseMessage
            {
                Success = false,
                code = "504",
                message = "Quá thời gian chờ Server hoàn tất HLS. Dữ liệu tạm được giữ lại."
            };
        }

        [HttpGet]
        public IActionResult GetBrowserHlsCompletionStatus(Guid uploadId)
        {
            if (!BrowserHlsCompletionJobs.TryGetValue(uploadId, out var job) || job.AdminUserId != GetCurrentAdminUserId())
                return NotFound(new CResponseMessage { Success = false, code = "404", message = "Không tìm thấy tác vụ hoàn tất HLS." });
            if (job.Status == "PROCESSING")
                return Accepted(new CResponseMessage { Success = true, code = "202", message = "Server đang ghép và lưu HLS." });

            BrowserHlsCompletionJobs.TryRemove(uploadId, out _);
            return job.Result?.Success == true ? Ok(job.Result) : StatusCode(500, job.Result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBrowserHlsUpload(Guid uploadId)
        {
            if (uploadId == Guid.Empty)
                return BadRequest(new { success = false, message = "UploadId không hợp lệ." });
            if (BrowserHlsCompletionJobs.TryGetValue(uploadId, out var job) && job.Status == "PROCESSING")
                return Conflict(new CResponseMessage { Success = false, code = "409", message = "Server đang hoàn tất HLS nên không thể hủy lúc này." });
            var result = await _videoSoureService.CancelBrowserHlsUploadAsync(uploadId);
            return result?.Success == true ? Ok(result) : StatusCode(500, result ?? new CResponseMessage
            {
                Success = false,
                code = "500",
                message = "Không nhận được phản hồi khi hủy HLS."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(16L * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 16L * 1024 * 1024)]
        public async Task<IActionResult> UploadMp4Chunk(
            Guid uploadId,
            int chunkIndex,
            int totalChunks,
            [FromForm] string fileFingerprint,
            [FromForm(Name = "Chunk")] IFormFile? chunk)
        {
            if (uploadId == Guid.Empty || chunkIndex < 0 || totalChunks < 1 || chunkIndex >= totalChunks)
                return BadRequest(new { success = false, message = "Thông tin chunk không hợp lệ." });
            if (chunk == null || chunk.Length == 0 || chunk.Length > 8L * 1024 * 1024)
                return BadRequest(new { success = false, message = "Chunk rỗng hoặc lớn hơn 8 MiB." });

            var result = await _videoSoureService.UploadMp4ChunkAsync(uploadId, chunkIndex, totalChunks, fileFingerprint, chunk);
            return result.Success ? Ok(result) : StatusCode(500, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetMp4UploadStatus(Guid uploadId, string fileFingerprint)
        {
            if (uploadId == Guid.Empty || string.IsNullOrWhiteSpace(fileFingerprint))
                return BadRequest(new { success = false, message = "Thông tin resume upload không hợp lệ." });

            var result = await _videoSoureService.GetMp4UploadStatusAsync(uploadId, fileFingerprint);
            return Ok(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteMp4Upload(Guid uploadId, [FromBody] CompleteMp4VideoUploadDto request)
        {
            if (uploadId == Guid.Empty || request == null)
                return BadRequest(new { success = false, message = "Thông tin hoàn tất upload không hợp lệ." });

            var result = await _videoSoureService.CompleteMp4UploadAsync(uploadId, request);
            return result != null && result.Success ? Ok(result) : StatusCode(500, result ?? new CompleteMp4UploadResultDto { Success = false, code = "500", message = "Không nhận được phản hồi từ hệ thống." });
        }

        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CancelMp4Upload([FromQuery] Guid? uploadId, [FromForm(Name = "uploadId")] Guid? formUploadId)
        {
            var effectiveUploadId = uploadId.HasValue && uploadId.Value != Guid.Empty
                ? uploadId.Value
                : formUploadId ?? Guid.Empty;

            if (effectiveUploadId == Guid.Empty)
                return BadRequest(new { success = false, message = "UploadId không hợp lệ." });

            await _videoSoureService.CancelMp4UploadAsync(effectiveUploadId);
            return Ok(new { success = true });
        }



        public async Task<IActionResult> GetAll()
        {
            var result = await _videoSoureService.get_all(GetOwnershipFilterUserId());
            return Ok(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MaxVideoUploadBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxVideoUploadBytes)]
        public async Task<IActionResult> Update(decimal sourceId, IFormFile? file, [FromForm] UpdateVideoSourceInputDto meta)
        {
            var result = await _videoSoureService.uppdate_VideoSoure(sourceId, file, meta);
            return Ok(result);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Delete([FromQuery] decimal? id, [FromForm(Name = "id")] decimal? formId, [FromBody] DeleteVideoSourceInputDto? body)
        {
            var effectiveId = id ?? formId ?? body?.SourceId ?? body?.Id ?? 0;
            if (effectiveId <= 0)
                return BadRequest(new { success = false, message = "ID nguồn video không hợp lệ." });

            var result = await _videoSoureService.Delete_video_source(effectiveId);
            return Ok(result);
        }
    }
}
