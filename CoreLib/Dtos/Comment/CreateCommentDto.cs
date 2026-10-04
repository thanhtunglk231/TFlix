namespace CoreLib.Dtos.Comment
{
    public class CreateCommentDto
    {
        public long? MovieId { get; set; }
        public long? EpisodeId { get; set; }
        public long UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserAvatar { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
