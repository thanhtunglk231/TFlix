using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Services.VideoHighlights;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN,SUPER_ADMIN")]
public sealed class VideoHighlightsController : ControllerBase
{
    private readonly ICVideoHighlight _repository;
    private readonly IVideoHighlightGenerator _generator;
    private readonly ICloudflareR2Service _r2;
    public VideoHighlightsController(ICVideoHighlight repository, IVideoHighlightGenerator generator, ICloudflareR2Service r2)
    { _repository = repository; _generator = generator; _r2 = r2; }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] bool activeOnly = false)
        => Ok(await _repository.GetAllAsync(page, pageSize, activeOnly));

    [HttpGet("source/{sourceId:long}")]
    public async Task<IActionResult> GetBySource(long sourceId) => Ok(await _repository.GetBySourceAsync(sourceId));

    [Authorize]
    [HttpPost("generate/{sourceId:long}")]
    public async Task<IActionResult> Generate(long sourceId, CancellationToken cancellationToken)
    {
        var result = await _generator.GenerateAsync(sourceId, cancellationToken);
        return result.Success ? Ok(result) : StatusCode(result.code == "400" ? 400 : 500, result);
    }

    [Authorize]
    [HttpDelete("{highlightId:long}")]
    public async Task<IActionResult> Delete(long highlightId, [FromQuery] string? videoUrl, [FromQuery] string? thumbnailUrl)
    {
        var result = await _repository.DeleteAsync(highlightId);
        if (result.Success)
        {
            if (!string.IsNullOrWhiteSpace(videoUrl)) await _r2.DeleteFileAsync(videoUrl);
            if (!string.IsNullOrWhiteSpace(thumbnailUrl)) await _r2.DeleteFileAsync(thumbnailUrl);
        }
        return Ok(result);
    }
}
