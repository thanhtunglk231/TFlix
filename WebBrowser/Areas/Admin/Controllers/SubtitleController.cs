using Microsoft.AspNetCore.Mvc;
using WebBrowser.Services.Interfaces;
using CoreLib.Dtos.Subtitle;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Antiforgery;

namespace WebBrowser.Areas.Admin.Controllers;

public sealed class SubtitleController : AdminBaseController
{
    private readonly ISubtitleService _service;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IAntiforgery _antiforgery;
    public SubtitleController(ISubtitleService service, IHttpClientFactory httpClientFactory,
        IConfiguration configuration, IAntiforgery antiforgery)
    {
        _service = service;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _antiforgery = antiforgery;
    }

    [HttpGet]
    public IActionResult RequestToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        return Json(new { requestToken = tokens.RequestToken });
    }

    [HttpGet]
    public async Task<IActionResult> ProxyHlsFile(long sourceId, string url, CancellationToken cancellationToken)
    {
        if (sourceId <= 0 || string.IsNullOrWhiteSpace(url)) return BadRequest("Thông tin file HLS không hợp lệ.");

        // Giải mã và trích xuất URL gốc nếu bị bọc proxy lặp
        while (url.Contains("ProxyHlsFile", StringComparison.OrdinalIgnoreCase) && url.Contains("url=", StringComparison.OrdinalIgnoreCase))
        {
            var match = System.Text.RegularExpressions.Regex.Match(url, @"[?&]url=([^&]+)");
            if (match.Success)
            {
                url = Uri.UnescapeDataString(match.Groups[1].Value);
            }
            else break;
        }

        var apiBaseUrl = _configuration["PathStrings:Url"]
            ?? throw new InvalidOperationException("Chưa cấu hình Server API URL.");
        var requestUrl = new Uri(new Uri(apiBaseUrl),
            $"/api/Subtitles/browser-hls?sourceId={sourceId}&url={Uri.EscapeDataString(url)}");
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        var token = HttpContext.Session.GetString("AdminJWToken") ?? HttpContext.Session.GetString("JWToken");
        if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await _httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, content.Length > 0 ? System.Text.Encoding.UTF8.GetString(content) : "Không tải được file HLS.");
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        if (url.Contains(".m3u8", StringComparison.OrdinalIgnoreCase))
        {
            var rawM3u8 = System.Text.Encoding.UTF8.GetString(content);
            var baseUrl = url.Substring(0, url.LastIndexOf('/') + 1);
            var lines = rawM3u8.Split('\n');
            var sb = new System.Text.StringBuilder();

            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd('\r');
                var trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#"))
                {
                    var fullSegmentUrl = trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                        ? trimmed
                        : baseUrl + trimmed;
                    var proxiedUrl = Url.Action("ProxyHlsFile", "Subtitle", new { area = "Admin", sourceId, url = fullSegmentUrl }, Request.Scheme);
                    sb.AppendLine(proxiedUrl ?? fullSegmentUrl);
                }
                else
                {
                    sb.AppendLine(line);
                }
            }
            Response.Headers["Access-Control-Allow-Origin"] = "*";
            return Content(sb.ToString(), "application/vnd.apple.mpegurl", System.Text.Encoding.UTF8);
        }
        Response.Headers["Access-Control-Allow-Origin"] = "*";
        return File(content, contentType);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(long sourceId) => Ok(await _service.GenerateAsync(sourceId));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartBrowserJob([FromBody] BrowserSubtitleJobRequestDto request) =>
        Ok(await _service.StartBrowserJobAsync(request));

    [HttpGet]
    public async Task<IActionResult> GetBrowserJob(Guid jobId)
    {
        if (jobId == Guid.Empty) return BadRequest(new { message = "Mã tác vụ không hợp lệ." });
        return Ok(await _service.GetBrowserJobAsync(jobId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(14 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 14 * 1024 * 1024)]
    public async Task<IActionResult> UploadBrowserChunk([FromQuery] Guid jobId, [FromQuery] int chunkIndex,
        [FromForm] double offsetSeconds, [FromForm] double durationSeconds, [FromForm] IFormFile? audio)
    {
        if (jobId == Guid.Empty || chunkIndex < 0 || audio is null || audio.Length == 0)
            return BadRequest(new { message = "Audio chunk hoặc thông tin tác vụ không hợp lệ." });
        return Ok(await _service.UploadBrowserChunkAsync(jobId, chunkIndex, offsetSeconds, durationSeconds, audio));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteBrowserJob(Guid jobId)
    {
        if (jobId == Guid.Empty) return BadRequest(new { message = "Mã tác vụ không hợp lệ." });
        return Ok(await _service.CompleteBrowserJobAsync(jobId));
    }

    [HttpGet]
    public async Task<IActionResult> GetDraft(Guid jobId)
    {
        if (jobId == Guid.Empty) return BadRequest(new { message = "Mã tác vụ không hợp lệ." });
        return Ok(await _service.GetDraftAsync(jobId));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDraft(Guid jobId, [FromBody] UpdateSubtitleDraftDto request)
    {
        if (jobId == Guid.Empty) return BadRequest(new { message = "Mã tác vụ không hợp lệ." });
        return Ok(await _service.UpdateDraftAsync(jobId, request));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid jobId)
    {
        if (jobId == Guid.Empty) return BadRequest(new { message = "Mã tác vụ không hợp lệ." });
        return Ok(await _service.ApproveAsync(jobId));
    }
}
