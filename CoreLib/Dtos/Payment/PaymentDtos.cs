using System.ComponentModel.DataAnnotations;

namespace CoreLib.Dtos.Payment;

public sealed class CheckoutPaymentDto
{
    [Range(1, long.MaxValue)]
    public long PlanId { get; set; }

}

public sealed class PayOsCheckoutResultDto
{
    public long OrderCode { get; set; }
    public long PaymentId { get; set; }
    public string CheckoutUrl { get; set; } = string.Empty;
    public string QrCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PlanName { get; set; } = string.Empty;
}

public sealed class SubscriptionPlanItemDto
{
    public long PlanId { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "VND";
    public int DurationDays { get; set; }
    public int MaxDevices { get; set; }
    public string? QualityCap { get; set; }
    public string AdsFree { get; set; } = "N";
    public string Downloadable { get; set; } = "N";
}

public sealed class SubscriptionStatusDto
{
    public bool IsActive { get; set; }
    public long? SubscriptionId { get; set; }
    public string? PlanName { get; set; }
    public DateTimeOffset? EndAt { get; set; }
}

