using System.ComponentModel.DataAnnotations;

namespace CoreLib.Dtos.Subscription;

public class SubscriptionPlanDto
{
    [Required, StringLength(50)]
    [RegularExpression("^[A-Z0-9_-]+$", ErrorMessage = "Mã gói chỉ gồm chữ in hoa, số, dấu gạch ngang hoặc gạch dưới.")]
    public string PlanCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "9999999999")]
    public decimal Price { get; set; }

    [Range(1, 3650)]
    public int DurationDays { get; set; }

    [Range(1, 20)]
    public int MaxDevices { get; set; } = 2;

    [StringLength(20)]
    public string? QualityCap { get; set; }

    public bool AdsFree { get; set; } = true;
    public bool Downloadable { get; set; }
}

public sealed class CreateSubscriptionPlanDto : SubscriptionPlanDto
{
}

public sealed class UpdateSubscriptionPlanDto : SubscriptionPlanDto
{
    [Range(1, long.MaxValue)]
    public long PlanId { get; set; }
}
