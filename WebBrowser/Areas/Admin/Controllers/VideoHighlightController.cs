using Microsoft.AspNetCore.Mvc;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Areas.Admin.Controllers;

public sealed class VideoHighlightController : AdminBaseController
{
    private readonly IVideoHighlightService _service;
    public VideoHighlightController(IVideoHighlightService service) => _service = service;
    public IActionResult Index() => View();
    [HttpGet]
    public async Task<IActionResult> GetAll(int page = 1) => Ok(await _service.GetAllAsync(page, 10));
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(long sourceId) => Ok(await _service.GenerateAsync(sourceId));
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long highlightId, string? videoUrl, string? thumbnailUrl)
        => Ok(await _service.DeleteAsync(highlightId, videoUrl, thumbnailUrl));
}
