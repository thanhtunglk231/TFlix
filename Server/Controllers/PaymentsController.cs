using System.Security.Claims;
using CoreLib.Dtos.Payment;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Server.Services;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace Server.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public sealed class PaymentsController : ControllerBase
{
    private readonly ICPayment _payments;
    private readonly IPayOsService _payOs;

    public PaymentsController(ICPayment payments, IPayOsService payOs)
    {
        _payments = payments;
        _payOs = payOs;
    }

    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlans() => ToActionResult(await _payments.GetPlansAsync());

    [HttpGet("subscription-status")]
    public async Task<IActionResult> GetSubscriptionStatus()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email)) return Unauthorized();
        return ToActionResult(await _payments.GetSubscriptionStatusAsync(email));
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutPaymentDto dto, CancellationToken cancellationToken)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email)) return Unauthorized();
        var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var pending = await _payments.CreatePendingAsync(email, dto.PlanId, orderCode);
        if (!pending.Success) return ToActionResult(pending);
        var row = ReadTable<PendingPaymentRow>(pending.Data).FirstOrDefault();
        if (row == null) return StatusCode(500, new { success = false, message = "Không đọc được giao dịch PayOS." });

        try
        {
            var description = $"TFLIX{orderCode % 10000:0000}";
            var link = await _payOs.CreatePaymentLinkAsync(orderCode, decimal.ToInt32(row.Amount), description, cancellationToken);
            return Ok(new
            {
                success = true,
                code = "200",
                message = "Đã tạo mã QR PayOS.",
                data = new PayOsCheckoutResultDto
                {
                    OrderCode = orderCode,
                    PaymentId = row.PaymentId,
                    CheckoutUrl = link.CheckoutUrl,
                    QrCode = link.QrCode,
                    Amount = row.Amount,
                    PlanName = row.PlanName
                }
            });
        }
        catch
        {
            await _payments.FailPayOsAsync(orderCode);
            return StatusCode(502, new { success = false, code = "502", message = "Không thể tạo mã QR PayOS. Vui lòng thử lại." });
        }
    }

    [AllowAnonymous]
    [HttpPost("/api/payment/payos/webhook")]
    public async Task<IActionResult> PayOsWebhook([FromBody] JObject payload)
    {
        if (payload["data"] is not JObject dataObject || payload.Value<string>("signature") is not { Length: > 0 } signature)
            return BadRequest(new { success = false });
        using var dataDocument = JsonDocument.Parse(dataObject.ToString(Formatting.None));
        var data = dataDocument.RootElement;
        if (!_payOs.VerifyWebhook(data, signature)) return BadRequest(new { success = false, message = "Invalid signature" });

        var orderCode = data.GetProperty("orderCode").GetInt64();
        var amount = data.GetProperty("amount").GetDecimal();
        var code = data.TryGetProperty("code", out var codeElement) ? codeElement.GetString() : null;
        if (code == "00") await _payments.CompletePayOsAsync(orderCode, amount);
        else await _payments.FailPayOsAsync(orderCode);
        return Ok(new { success = true });
    }

    [HttpPost("payos/confirm/{orderCode:long}")]
    public async Task<IActionResult> ConfirmPayOs(long orderCode, CancellationToken cancellationToken)
    {
        var payment = await _payOs.GetPaymentAsync(orderCode, cancellationToken);
        if (!string.Equals(payment.Status, "PAID", StringComparison.OrdinalIgnoreCase))
            return Conflict(new { success = false, code = "409", message = "PayOS chưa xác nhận thanh toán." });
        return ToActionResult(await _payments.CompletePayOsAsync(orderCode, payment.AmountPaid));
    }

    private static List<T> ReadTable<T>(object? data)
    {
        if (data == null) return [];
        return JsonConvert.DeserializeObject<TableEnvelope<T>>(JsonConvert.SerializeObject(data))?.Table ?? [];
    }

    private sealed class TableEnvelope<T> { public List<T> Table { get; set; } = []; }
    private sealed class PendingPaymentRow
    {
        public long PaymentId { get; set; }
        public decimal Amount { get; set; }
        public string PlanName { get; set; } = string.Empty;
    }

    private IActionResult ToActionResult(CoreLib.Models.CResponseMessage result) =>
        StatusCode(result.code switch { "200" => 200, "400" => 400, "404" => 404, "409" => 409, _ => 500 }, result);
}

