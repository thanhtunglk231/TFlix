using CoreLib.Dtos.CMS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Controllers
{
    public class NewsController : Controller
    {
        private readonly INewsService _newsService;

        public NewsController(INewsService newsService)
        {
            _newsService = newsService;
        }

        private bool IsUserAuthenticated()
        {
            var token = HttpContext.Session.GetString("JWToken");
            return !string.IsNullOrEmpty(token) || (User != null && User.Identity != null && User.Identity.IsAuthenticated);
        }

        public async Task<IActionResult> Index(string? q, int? categoryId, int page = 1)
        {
            // Kiểm tra Đăng nhập
            if (!IsUserAuthenticated())
            {
                string returnUrl = Url.Action("Index", "News", new { q, categoryId, page }) ?? "/News";
                return RedirectToAction("Index", "Auth", new { returnUrl });
            }

            var filter = new NewsCatalogFilterDto
            {
                Search = q,
                CategoryId = categoryId,
                Page = page < 1 ? 1 : page,
                PageSize = 9
            };

            var catalog = await _newsService.GetCatalogNewsAsync(filter);
            var categories = await _newsService.GetCategoriesAsync();

            ViewBag.Filter = filter;
            ViewBag.Categories = categories;

            return View(catalog);
        }

        public async Task<IActionResult> Detail(long id)
        {
            // Kiểm tra Đăng nhập
            if (!IsUserAuthenticated())
            {
                string returnUrl = Url.Action("Detail", "News", new { id }) ?? "/News";
                return RedirectToAction("Index", "Auth", new { returnUrl });
            }

            if (id <= 0) return RedirectToAction("Index");

            var newsDetail = await _newsService.GetNewsDetailAsync(id);
            if (newsDetail == null)
            {
                return NotFound("Bài viết không tồn tại hoặc đã bị xóa.");
            }

            var categories = await _newsService.GetCategoriesAsync();
            ViewBag.Categories = categories;

            return View(newsDetail);
        }

        public IActionResult Promotions()
        {
            if (!IsUserAuthenticated())
            {
                return RedirectToAction("Index", "Auth", new { returnUrl = "/News?categoryId=4" });
            }
            return RedirectToAction("Index", new { categoryId = 4 });
        }
    }
}
