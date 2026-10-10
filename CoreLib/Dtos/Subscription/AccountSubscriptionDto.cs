using System.ComponentModel.DataAnnotations;

namespace CoreLib.Dtos.Subscription;

public class SaveAccountSubscriptionDto
{
    [Range(1, long.MaxValue)]
    public long UserId { get; set; }

    [Range(1, long.MaxValue)]
    public long PlanId { get; set; }

    public DateTimeOffset StartAt { get; set; }

    public DateTimeOffset EndAt { get; set; }

    [Required, RegularExpression("^(ACTIVE|EXPIRED|CANCELLED)$")]
    public string Status { get; set; } = "ACTIVE";
}

public sealed class CreateAccountSubscriptionDto : SaveAccountSubscriptionDto
{
}

public sealed class UpdateAccountSubscriptionDto : SaveAccountSubscriptionDto
{
    [Range(1, long.MaxValue)]
    public long SubscriptionId { get; set; }
}
