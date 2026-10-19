namespace CoreLib.Dtos.VideoUploadRequest;

public class ReviewVideoUploadRequestDto
{
    public long RequestId { get; set; }
    public string Status { get; set; } = "APPROVED"; // APPROVED, REJECTED
    public string? AdminNotes { get; set; }
    public long? AdminUserId { get; set; }
    public long? ReviewedBy { get => AdminUserId; set => AdminUserId = value; }
}
