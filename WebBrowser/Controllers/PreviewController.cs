using CoreLib.Dtos.Preview;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Models.Preview;
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

        public async Task<IActionResult> Details(int id, string kind)
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
            if (resp == null)
            {
                return NotFound("Không lấy được phản hồi từ service");
            }

            if (!resp.success)
            {
                // Forward message from API if any
                return NotFound(resp.message ?? "Không tìm thấy nội dung");
            }

            if (resp.Data == null || resp.Data.Table == null || resp.Data.Table.Count == 0)
            {
                return NotFound("Không tìm thấy nội dung");
            }

            // Safe access first item
            var movie = resp.Data.Table[0];

            // View Index.cshtml đang khai báo @model PreviewItem
            return View("Index", movie);
        }

        public async Task<IActionResult> getpreview([FromQuery]GETCONTENTByID movie)
        {
            var reusult = await _previewService.get_preview(movie);
            return Json(reusult);
        }




        public async Task<IActionResult> Episode(int episodeId)
        {
            Console.WriteLine($"[Preview.Episode] episodeId={episodeId}");

            var resp = await _previewService.get_episode(episodeId);

            Console.WriteLine("[Preview.Episode] resp = " + JsonConvert.SerializeObject(resp));

            if (resp == null)
            {
                return NotFound("Không lấy được phản hồi từ service");
            }

            if (!resp.success)
            {
                return NotFound(resp.message ?? "Không tìm thấy tập phim");
            }

            if (resp.Data == null || resp.Data.Table == null || resp.Data.Table.Count == 0)
            {
                return NotFound("Không có dữ liệu tập phim");
            }

            var episode = resp.Data.Table[0];

            return View("Index", episode); // dùng chung view với movie
        }
        //public async Task<IActionResult> GetMovie([FromQuery] int id)
        //{
        //    var result = await _previewService.(id);
        //        return Ok(result);
        //}
    }
}
