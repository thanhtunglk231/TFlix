using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using CoreLib.Dtos.Subtitle;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Services.Subtitles;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubtitlesController : ControllerBase
    {
        private readonly ISubtitleService _subtitleService;
        private readonly ICSubtitle _repository;
        private readonly ISubtitleGenerator _generator;
        private readonly IBrowserSubtitleJobService _browserJobs;

        public SubtitlesController(ISubtitleService subtitleService, ICSubtitle repository, ISubtitleGenerator generator,
            IBrowserSubtitleJobService browserJobs)
        {
            _subtitleService = subtitleService;
            _repository = repository;
            _generator = generator;
            _browserJobs = browserJobs;
        }

        private string OwnerId => User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub") ?? User.Identity?.Name ?? "unknown-admin";

        [HttpPost("browser-jobs")]
        [Authorize(Roles = "ADMIN,SUPER_ADMIN")]
        public async Task<IActionResult> StartBrowserJob([FromBody] BrowserSubtitleJobRequestDto request,
            CancellationToken cancellationToken)
        {
            try { return Ok(await _browserJobs.StartAsync(request, OwnerId, cancellationToken)); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("browser-hls")]
        [Authorize(Roles = "ADMIN,SUPER_ADMIN")]
        public async Task<IActionResult> DownloadBrowserHlsFile([FromQuery] long sourceId, [FromQuery] string url,
            CancellationToken cancellationToken)
        {
            try
            {
                var file = await _browserJobs.DownloadHlsFileAsync(sourceId, url, cancellationToken);
                return File(file.Content, file.ContentType);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("browser-jobs/{jobId:guid}")]
        [Authorize(Roles = "ADMIN,SUPER_ADMIN")]
        public async Task<IActionResult> GetBrowserJob(Guid jobId, CancellationToken cancellationToken)
        {
            var job = await _browserJobs.GetAsync(jobId, OwnerId, cancellationToken);
            return job is null ? NotFound() : Ok(job);
        }

        [HttpPost("browser-jobs/{jobId:guid}/chunks/{chunkIndex:int}")]
        [Authorize(Roles = "ADMIN,SUPER_ADMIN")]
        [RequestSizeLimit(14 * 1024 * 1024)]
        public async Task<IActionResult> UploadBrowserChunk(Guid jobId, int chunkIndex,
            [FromForm] double offsetSeconds, [FromForm] double durationSeconds, [FromForm] IFormFile audio,
            CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _browserJobs.ProcessChunkAsync(jobId, chunkIndex, offsetSeconds,
                    durationSeconds, audio, OwnerId, cancellationToken));
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (System.OperationCanceledException)
            {
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status408RequestTimeout,
                    new { message = "Yêu cầu xử lý audio chunk đã hết thời gian chờ hoặc bị hủy." });
            }
        }

        [HttpPost("browser-jobs/{jobId:guid}/complete")]
        [Authorize(Roles = "ADMIN,SUPER_ADMIN")]
        public async Task<IActionResult> CompleteBrowserJob(Guid jobId, CancellationToken cancellationToken)
        {
            try { return Ok(await _browserJobs.CompleteAsync(jobId, OwnerId, cancellationToken)); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("browser-jobs/{jobId:guid}/draft")]
        [Authorize(Roles = "ADMIN,SUPER_ADMIN")]
        public async Task<IActionResult> GetDraft(Guid jobId, CancellationToken cancellationToken)
        {
            try { return Ok(await _browserJobs.GetDraftAsync(jobId, OwnerId, cancellationToken)); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("browser-jobs/{jobId:guid}/draft")]
        [Authorize(Roles = "ADMIN,SUPER_ADMIN")]
        public async Task<IActionResult> UpdateDraft(Guid jobId, [FromBody] UpdateSubtitleDraftDto request,
            CancellationToken cancellationToken)
        {
            try { return Ok(await _browserJobs.UpdateDraftAsync(jobId, request, OwnerId, cancellationToken)); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("browser-jobs/{jobId:guid}/approve")]
        [Authorize(Roles = "ADMIN,SUPER_ADMIN")]
        public async Task<IActionResult> Approve(Guid jobId, CancellationToken cancellationToken)
        {
            try { return Ok(await _browserJobs.ApproveAsync(jobId, OwnerId, cancellationToken)); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("source/{sourceId:long}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBySource(long sourceId) => Ok(await _repository.GetBySourceAsync(sourceId));

        [HttpPost("source/{sourceId:long}/save")]
        [Authorize(Roles = "ADMIN,SUPER_ADMIN")]
        public async Task<IActionResult> SaveSourceSubtitles(long sourceId, [FromBody] SaveSourceSubtitlesDto request, CancellationToken cancellationToken)
        {
            try
            {
                if (sourceId <= 0 || request.Cues == null || request.Cues.Count == 0)
                    return BadRequest(new { message = "Dữ liệu phụ đề không hợp lệ." });
                var success = await _browserJobs.SaveSourceSubtitlesAsync(sourceId, request.Cues, cancellationToken);
                return Ok(new { success, message = "Đã lưu chỉnh sửa phụ đề thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpPost("generate/{sourceId:long}")]
        [Authorize(Roles = "ADMIN,SUPER_ADMIN")]
        public async Task<IActionResult> GenerateForSource(long sourceId, CancellationToken cancellationToken)
        {
            var result = await _generator.GenerateAsync(sourceId, cancellationToken);
            return result.Success ? Ok(result) : StatusCode(result.code == "400" ? 400 : 500, result);
        }

        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] GenerateSubtitleRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _subtitleService.GetOrGenerateSubtitlesAsync(request, cancellationToken);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] long? movieId,
            [FromQuery] long? episodeId,
            [FromQuery] string? title,
            [FromQuery] string? overview,
            [FromQuery] double? durationSeconds,
            CancellationToken cancellationToken)
        {
            var request = new GenerateSubtitleRequestDto
            {
                MovieId = movieId,
                EpisodeId = episodeId,
                Title = title,
                Overview = overview,
                DurationSeconds = durationSeconds,
                ForceRegenerate = false
            };

            var result = await _subtitleService.GetOrGenerateSubtitlesAsync(request, cancellationToken);
            return Ok(result);
        }

        [HttpGet("vtt")]
        public async Task<IActionResult> GetWebVtt(
            [FromQuery] long? movieId,
            [FromQuery] long? episodeId,
            [FromQuery] string? lang,
            CancellationToken cancellationToken)
        {
            var vtt = await _subtitleService.GetWebVttContentAsync(movieId, episodeId, lang ?? "vi", cancellationToken);
            return Content(vtt, "text/vtt; charset=utf-8");
        }
    }
}
