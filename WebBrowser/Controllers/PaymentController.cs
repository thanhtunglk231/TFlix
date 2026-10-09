using CoreLib.Dtos.Payment;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Models.Payment;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Controllers;

public sealed class PaymentController : Controller
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService) => _paymentService = paymentService;

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
        return View("PayOsQr", new PaymentViewModel { PayOsPayment = payment, SelectedPlanId = dto.PlanId, ReturnUrl = LocalReturnUrl(returnUrl) });
    }

    [HttpGet]
    public async Task<IActionResult> Success(long orderCode, string? status)
    {
        if (!string.Equals(status, "PAID", StringComparison.OrdinalIgnoreCase)) return RedirectToAction("Index", "Home");
        var result = await _paymentService.ConfirmPayOsAsync(orderCode);
        if (result.Success) TempData["PaymentSuccess"] = "Thanh toán PayOS thành công. Tài khoản đã được nâng cấp.";
        else TempData["PaymentError"] = result.message;
        return RedirectToAction("Index", "Home");
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
            return Json(new { paid = result.Success });
        }
        catch
        {
            return Json(new { paid = false });
        }
    }

    private string LocalReturnUrl(string? returnUrl) => Url.IsLocalUrl(returnUrl) ? returnUrl! : "/";

    private static List<T> ReadTable<T>(object? data)
    {
        if (data == null) return [];
        var wrapper = JsonConvert.DeserializeObject<TableEnvelope<T>>(JsonConvert.SerializeObject(data));
        return wrapper?.Table ?? [];
    }

    private sealed class TableEnvelope<T> { public List<T> Table { get; set; } = []; }
}

