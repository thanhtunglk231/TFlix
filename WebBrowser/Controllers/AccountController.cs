using CoreLib.Dtos.Payment;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Models.AuthModels;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Controllers;

public class AccountController : Controller
{
    private readonly IFavoriteService _favoriteService;
    private readonly IPaymentService _paymentService;

    public AccountController(IFavoriteService favoriteService, IPaymentService paymentService)
    {
        _favoriteService = favoriteService;
        _paymentService = paymentService;
    }

    public async Task<IActionResult> Profile()
    {
        var json = HttpContext.Session.GetString("CurrentUser");
        if (string.IsNullOrWhiteSpace(json)) return RedirectToAction("Index", "Auth", new { returnUrl = Url.Action("Profile", "Account") });

        var user = JsonConvert.DeserializeObject<UserInfo>(json);
        if (user == null || user.userId <= 0) return RedirectToAction("Index", "Auth");

        var subscriptionStatus = new SubscriptionStatusDto();
        if (!string.IsNullOrWhiteSpace(HttpContext.Session.GetString("JWToken")))
        {
            try
            {
                var response = await _paymentService.GetSubscriptionStatusAsync();
                subscriptionStatus = ReadTable<SubscriptionStatusDto>(response.Data).FirstOrDefault() ?? subscriptionStatus;
            }
            catch
            {
                // Không chặn màn hình hồ sơ nếu API subscription tạm thời không khả dụng.
            }
        }

        ViewBag.CurrentUser = user;
        ViewBag.SubscriptionStatus = subscriptionStatus;
        return View(await _favoriteService.GetMoviesAsync(user.userId));
    }

    private static List<T> ReadTable<T>(object? data)
    {
        if (data == null) return [];
        var wrapper = JsonConvert.DeserializeObject<TableEnvelope<T>>(JsonConvert.SerializeObject(data));
        return wrapper?.Table ?? [];
    }

    private sealed class TableEnvelope<T>
    {
        public List<T> Table { get; set; } = [];
    }
}
