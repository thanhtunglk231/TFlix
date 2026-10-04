using System;

namespace CoreLib.Dtos.Comment
{
    public class CommentDto
    {
        public long CommentId { get; set; }
        public long? MovieId { get; set; }
        public long? EpisodeId { get; set; }
        public long UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserAvatar { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public string? FormattedTime { get; set; }
    }
}
