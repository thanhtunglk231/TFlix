namespace CoreLib.Dtos.Support
{
    public class SupportMessageDto
    {
        public string MessageId { get; set; } = Guid.NewGuid().ToString("N");
        public string SessionId { get; set; } = string.Empty;
        public long? UserId { get; set; }
        public string SenderName { get; set; } = "Khách hàng";
        public string SenderRole { get; set; } = "user"; // "user" hoặc "agent"
        public string? AvatarUrl { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string FormattedTime => CreatedAt.ToLocalTime().ToString("HH:mm");
    }

    public class SendSupportMessageRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public long? UserId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? MovieTitle { get; set; }
        public long? EpisodeId { get; set; }
    }
}
