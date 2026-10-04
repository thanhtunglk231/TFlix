using CoreLib.Dtos.VideSoure;
using CoreLib.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Areas.Admin.Controllers
{

    public class VideoSoureController : AdminBaseController
    {
        private const long MaxVideoUploadBytes = 1024L * 1024 * 1024;
        private readonly IVideoSoureService _videoSoureService;

        public VideoSoureController(IVideoSoureService videoSoureService)
        {
            _videoSoureService = videoSoureService;
        }
        public IActionResult Index()
        {
            return View();
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
            return result.Success ? Ok(result) : StatusCode(500, result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelMp4Upload(Guid uploadId)
        {
            if (uploadId == Guid.Empty)
                return BadRequest(new { success = false, message = "UploadId không hợp lệ." });

            await _videoSoureService.CancelMp4UploadAsync(uploadId);
            return Ok(new { success = true });
        }



        public async Task<IActionResult> GetAll()
        {
            var result = await _videoSoureService.get_all();
            return Ok(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MaxVideoUploadBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxVideoUploadBytes)]
        public async Task<IActionResult> Update(decimal sourceId, IFormFile file, [FromForm] UpdateVideoSourceInputDto meta)
        {
            var result = await _videoSoureService.uppdate_VideoSoure(sourceId, file, meta);
            return Ok(result);
        }
    }
}
