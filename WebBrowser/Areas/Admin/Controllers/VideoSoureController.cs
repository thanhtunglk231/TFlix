using CoreLib.Dtos.VideSoure;
using Microsoft.AspNetCore.Mvc;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Areas.Admin.Controllers
{

    [Area("Admin")]
    public class VideoSoureController : Controller
    {
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
        public async Task<IActionResult> Add(IFormFile file, [FromQuery] AddVideoSourceInputDto addVideoSourceInputDto)
        {
            var result = await _videoSoureService.add_VideoSoure(file, addVideoSourceInputDto);
            return Ok(result);
        }

        public async Task<IActionResult> GetAll()
        {
            var result = await _videoSoureService.get_all();
            return Ok(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(decimal sourceId, IFormFile file, [FromForm] UpdateVideoSourceInputDto meta)
        {
            var result = await _videoSoureService.uppdate_VideoSoure(sourceId, file, meta);
            return Ok(result);
        }
    }
}
