using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Models.AuthModels;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Controllers;

public class FavoriteController : Controller
{
    private readonly IFavoriteService _favoriteService;
    public FavoriteController(IFavoriteService favoriteService) => _favoriteService = favoriteService;

    [HttpGet]
    public async Task<IActionResult> MovieIds()
    {
        var userId = CurrentUserId();
        if (userId <= 0) return Unauthorized(new { success = false, loginUrl = Url.Action("Index", "Auth") });
        return Json(new { success = true, data = await _favoriteService.GetMovieIdsAsync(userId) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleMovie(long movieId)
    {
        var userId = CurrentUserId();
        if (userId <= 0) return Unauthorized(new { success = false, message = "Vui lòng đăng nhập để lưu phim yêu thích.", loginUrl = Url.Action("Index", "Auth") });
        var result = await _favoriteService.ToggleMovieAsync(userId, movieId);
        return result == null
            ? StatusCode(500, new { success = false, message = "Không thể cập nhật danh sách yêu thích." })
            : Json(new { success = true, data = result });
    }

    private long CurrentUserId()
    {
        var json = HttpContext.Session.GetString("CurrentUser");
        if (string.IsNullOrWhiteSpace(json)) return 0;
        try { return JsonConvert.DeserializeObject<UserInfo>(json)?.userId ?? 0; }
        catch { return 0; }
    }
}
