using Microsoft.AspNetCore.Mvc;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Controllers
{
    public class PreviewController : Controller
    {

        private readonly IPreviewService _previewService;

        public PreviewController(IPreviewService previewService)
        {
            _previewService = previewService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> GetMovie([FromQuery] int id)
        {
            var result = await _previewService.get_movie(id);
                return Ok(result);
        }
    }
}
