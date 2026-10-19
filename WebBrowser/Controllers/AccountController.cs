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
    private readonly IVideoUploadRequestService _videoUploadRequestService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IFavoriteService favoriteService,
        IPaymentService paymentService,
        IVideoUploadRequestService videoUploadRequestService,
        ILogger<AccountController> logger)
    {
        _favoriteService = favoriteService;
        _paymentService = paymentService;
        _videoUploadRequestService = videoUploadRequestService;
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

        var isPremium = activeSubscriptions.Any();
        if (isPremium)
        {
            try
            {
                var uploadRequest = await _videoUploadRequestService.GetStatusAsync(user.userId);
                ViewBag.VideoUploadRequest = uploadRequest;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load video upload request status for user {UserId}", user.userId);
            }
        }

        ViewBag.CurrentUser = user;
        ViewBag.ActiveSubscriptions = activeSubscriptions;
        ViewBag.SubscriptionStatus = activeSubscriptions.FirstOrDefault() ?? new SubscriptionStatusDto();
        return View(await _favoriteService.GetMoviesAsync(user.userId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestUpload([FromForm] string? reason)
    {
        var json = HttpContext.Session.GetString("CurrentUser");
        if (string.IsNullOrWhiteSpace(json))
            return Json(new { success = false, message = "Vui lòng đăng nhập lại." });

        var user = JsonConvert.DeserializeObject<UserInfo>(json);
        if (user == null || user.userId <= 0)
            return Json(new { success = false, message = "Phiên đăng nhập không hợp lệ." });

        // Kiểm tra quyền Premium
        var isPremium = false;
        try
        {
            var subResponse = await _paymentService.GetSubscriptionStatusAsync();
            if (subResponse.Success)
            {
                var subs = ReadTable<SubscriptionStatusDto>(subResponse.Data);
                isPremium = subs.Any(s => s.IsActive && s.SubscriptionId.HasValue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking premium status for user {UserId}", user.userId);
        }

        if (!isPremium)
        {
            return Json(new { success = false, message = "Chỉ tài khoản Premium mới có quyền gửi yêu cầu Upload video." });
        }

        var result = await _videoUploadRequestService.SubmitRequestAsync(new CoreLib.Dtos.VideoUploadRequest.CreateVideoUploadRequestDto
        {
            UserId = user.userId,
            Reason = reason
        });

        if (result == null)
        {
            return Json(new { success = false, message = "Không thể kết nối đến máy chủ. Vui lòng thử lại sau." });
        }

        return Json(new { success = result.Success, message = result.message });
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
