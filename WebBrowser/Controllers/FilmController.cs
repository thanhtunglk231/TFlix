using CoreLib.Dtos.Preview;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Models.Preview;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Controllers
{
    public class FilmController : Controller
    {

        private readonly IPreviewService _previewService;
        public FilmController(IPreviewService previewService)
        {
            _previewService = previewService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Watch(int id, string kind)
        {
            var request = new GETCONTENTByID
            {
                id = id,
                kind = kind
            };

            Console.WriteLine($"[Preview.Details] id={id}, kind={kind}");

            var resp = await _previewService.get_preview(request);
            Console.WriteLine("[Preview.Details] resp = " + JsonConvert.SerializeObject(resp));

            // resp: ApiResponse<PreviewTableWrapper>
            if (resp == null || resp.Data == null)
            {
                return NotFound("Không tìm thấy nội dung");
            }

            PreviewItem movie = resp.Data.Table[0];
            Console.WriteLine(JsonConvert.SerializeObject(movie));
            // View Index.cshtml đang khai báo @model PreviewItem
            return View("Index", movie);
        }


    }
}
