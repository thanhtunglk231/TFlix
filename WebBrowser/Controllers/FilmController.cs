using CoreLib.Dtos.Preview;
using Microsoft.AspNetCore.Http;
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

        public async Task<IActionResult> Watch(long id, string kind)
        {
            // Kiểm tra Đăng nhập (Authentication check)
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token) && (User == null || !User.Identity.IsAuthenticated))
            {
                // Chưa đăng nhập -> Chuyển hướng sang trang đăng nhập
                string returnUrl = Url.Action("Watch", "Film", new { id, kind }) ?? "/Movies";
                return RedirectToAction("Index", "Auth", new { returnUrl });
            }

            var request = new GETCONTENTByID
            {
                id = id,
                kind = string.IsNullOrEmpty(kind) ? "movie" : kind
            };

            Console.WriteLine($"[FilmController.Watch] id={id}, kind={kind}");

            var resp = await _previewService.get_preview(request);
            Console.WriteLine("[FilmController.Watch] resp = " + JsonConvert.SerializeObject(resp));

            if (resp == null || resp.Data == null || resp.Data.Table == null || resp.Data.Table.Count == 0)
            {
                return NotFound("Không tìm thấy nội dung xem phim.");
            }

            PreviewItem movie = resp.Data.Table[0];
            return View("Index", movie);
        }
    }
}
