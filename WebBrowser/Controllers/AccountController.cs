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
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IFavoriteService favoriteService,
        IPaymentService paymentService,
        ILogger<AccountController> logger)
    {
        _favoriteService = favoriteService;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task<IActionResult> Profile()
    {
        var json = HttpContext.Session.GetString("CurrentUser");
        if (string.IsNullOrWhiteSpace(json)) return RedirectToAction("Index", "Auth", new { returnUrl = Url.Action("Profile", "Account") });

        var user = JsonConvert.DeserializeObject<UserInfo>(json);
        if (user == null || user.userId <= 0) return RedirectToAction("Index", "Auth");

        var activeSubscriptions = new List<SubscriptionStatusDto>();
        if (!string.IsNullOrWhiteSpace(HttpContext.Session.GetString("JWToken")))
        {
            try
            {
                var response = await _paymentService.GetSubscriptionStatusAsync();
                if (response.Success)
                {
                    activeSubscriptions = ReadTable<SubscriptionStatusDto>(response.Data)
                        .Where(subscription => subscription.IsActive && subscription.SubscriptionId.HasValue)
                        .ToList();
                }
                else
                {
                    _logger.LogWarning(
                        "Could not load active subscriptions for account profile: {ResultCode} {Message}",
                        response.code,
                        response.message);
                    ViewBag.SubscriptionLoadError = "Chưa thể tải danh sách gói. Vui lòng tải lại trang.";
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Could not load active subscriptions for account profile");
                ViewBag.SubscriptionLoadError = "Chưa thể tải danh sách gói. Vui lòng tải lại trang.";
            }
        }

        ViewBag.CurrentUser = user;
        ViewBag.ActiveSubscriptions = activeSubscriptions;
        ViewBag.SubscriptionStatus = activeSubscriptions.FirstOrDefault() ?? new SubscriptionStatusDto();
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
