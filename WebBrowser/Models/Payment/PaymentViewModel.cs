using CoreLib.Dtos.Payment;

namespace WebBrowser.Models.Payment;

public sealed class PaymentViewModel
{
    public List<SubscriptionPlanItemDto> Plans { get; set; } = [];
    public long? SelectedPlanId { get; set; }
    public string ReturnUrl { get; set; } = "/";
    public string? ErrorMessage { get; set; }
    public PayOsCheckoutResultDto? PayOsPayment { get; set; }
}

