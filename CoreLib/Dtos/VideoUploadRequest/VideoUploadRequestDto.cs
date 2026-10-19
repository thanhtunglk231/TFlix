using System;

namespace CoreLib.Dtos.VideoUploadRequest;

public class VideoUploadRequestDto
{
    public long RequestId { get; set; }
    public long UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string? PlanName { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED
    public string? AdminNotes { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public long? ReviewedBy { get; set; }
}
