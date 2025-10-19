using CoreLib.Dtos.VideSoure;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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
        public async Task<IActionResult> Add(
      [FromForm(Name = "File")] IFormFile file,               // khớp key "File" trong FormData
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
