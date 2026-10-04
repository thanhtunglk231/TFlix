using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Models.AuthModels;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Controllers;

public class AccountController : Controller
{
    private readonly IFavoriteService _favoriteService;
    public AccountController(IFavoriteService favoriteService) => _favoriteService = favoriteService;

    public async Task<IActionResult> Profile()
    {
        var json = HttpContext.Session.GetString("CurrentUser");
        if (string.IsNullOrWhiteSpace(json)) return RedirectToAction("Index", "Auth", new { returnUrl = Url.Action("Profile", "Account") });
        var user = JsonConvert.DeserializeObject<UserInfo>(json);
        if (user == null || user.userId <= 0) return RedirectToAction("Index", "Auth");
        ViewBag.CurrentUser = user;
        return View(await _favoriteService.GetMoviesAsync(user.userId));
    }
}
