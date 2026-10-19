namespace CoreLib.Dtos.VideoUploadRequest;

public class CreateVideoUploadRequestDto
{
    public long UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string? PlanName { get; set; }
    public string? Reason { get; set; }
}
