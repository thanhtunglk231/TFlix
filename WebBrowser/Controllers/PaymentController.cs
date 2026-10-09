using CoreLib.Dtos.Payment;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Models.Payment;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Controllers;

public sealed class PaymentController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(long? planId, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("JWToken")))
            return RedirectToAction("Index", "Auth", new { returnUrl = Request.Path + Request.QueryString });

        var response = await _paymentService.GetPlansAsync();
        return View(new PaymentViewModel
        {
            Plans = ReadTable<SubscriptionPlanItemDto>(response.Data),
            SelectedPlanId = planId,
            ReturnUrl = LocalReturnUrl(returnUrl),
            ErrorMessage = response.Success ? null : response.message
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutPaymentDto dto, string? returnUrl)
    {
        if (!ModelState.IsValid)
            return RedirectToAction(nameof(Index), new { planId = dto.PlanId, returnUrl });

        var payment = await _paymentService.CheckoutAsync(dto);
        if (payment == null)
        {
            TempData["PaymentError"] = "Không thể tạo mã QR PayOS. Vui lòng thử lại.";
            return RedirectToAction(nameof(Index), new { planId = dto.PlanId, returnUrl });
        }

        HttpContext.Session.SetString($"PaymentReturnUrl:{payment.OrderCode}", LocalReturnUrl(returnUrl));
        return View("PayOsQr", new PaymentViewModel { PayOsPayment = payment, SelectedPlanId = dto.PlanId, ReturnUrl = LocalReturnUrl(returnUrl) });
    }

    [HttpGet]
    public async Task<IActionResult> Success(long orderCode, string? status)
    {
        if (!string.Equals(status, "PAID", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Cancel));

        return RedirectToAction(nameof(PaymentSuccess), new { orderCode });
    }

    [HttpGet]
    public async Task<IActionResult> PaymentSuccess(long orderCode)
    {
        if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("JWToken")))
            return RedirectToAction("Index", "Auth", new { returnUrl = Request.Path + Request.QueryString });

        var confirmation = await _paymentService.ConfirmPayOsAsync(orderCode);
        if (!confirmation.Success)
        {
            TempData["PaymentError"] = confirmation.message;
            return RedirectToAction(nameof(Index), new { returnUrl = GetPaymentReturnUrl(orderCode) });
        }

        List<SubscriptionStatusDto> activeSubscriptions = [];
        try
        {
            var subscriptionResponse = await _paymentService.GetSubscriptionStatusAsync();
            if (subscriptionResponse.Success)
            {
                activeSubscriptions = ReadTable<SubscriptionStatusDto>(subscriptionResponse.Data)
                    .Where(subscription => subscription.IsActive && subscription.SubscriptionId.HasValue)
                    .ToList();
            }
            else
            {
                _logger.LogWarning(
                    "Payment was confirmed for order {OrderCode}, but active subscriptions could not be loaded: {ResultCode} {Message}",
                    orderCode,
                    subscriptionResponse.code,
                    subscriptionResponse.message);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Payment was confirmed for order {OrderCode}, but active subscriptions could not be loaded", orderCode);
        }

        return View(new PaymentSuccessViewModel
        {
            OrderCode = orderCode,
            ReturnUrl = GetPaymentReturnUrl(orderCode),
            ActiveSubscriptions = activeSubscriptions
        });
    }

    [HttpGet]
    public IActionResult Cancel()
    {
        TempData["PaymentError"] = "Thanh toán PayOS chưa thành công hoặc đã bị hủy.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Status(long orderCode)
    {
        try
        {
            var result = await _paymentService.ConfirmPayOsAsync(orderCode);
            return Json(new
            {
                paid = result.Success,
                successUrl = result.Success
                    ? Url.Action(nameof(PaymentSuccess), new { orderCode })
                    : null
            });
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not verify PayOS payment status for order {OrderCode}", orderCode);
            return Json(new { paid = false });
        }
    }

    private string LocalReturnUrl(string? returnUrl) => Url.IsLocalUrl(returnUrl) ? returnUrl! : "/";

    private string GetPaymentReturnUrl(long orderCode)
    {
        var key = $"PaymentReturnUrl:{orderCode}";
        var returnUrl = HttpContext.Session.GetString(key);
        HttpContext.Session.Remove(key);
        return LocalReturnUrl(returnUrl);
    }

    private static List<T> ReadTable<T>(object? data)
    {
        if (data == null) return [];
        var wrapper = JsonConvert.DeserializeObject<TableEnvelope<T>>(JsonConvert.SerializeObject(data));
        return wrapper?.Table ?? [];
    }

    private sealed class TableEnvelope<T> { public List<T> Table { get; set; } = []; }
}
