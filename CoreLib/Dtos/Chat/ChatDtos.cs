using System;
using System.Collections.Generic;

namespace CoreLib.Dtos.Chat
{
    public class ChatMovieCardDto
    {
        public long Id { get; set; }
        public string Kind { get; set; } = "movie"; // "movie" hoặc "series"
        public string Title { get; set; } = string.Empty;
        public string? OriginalTitle { get; set; }
        public string? PosterUrl { get; set; }
        public decimal? Rating { get; set; } = 8.5m;
        public string? Genres { get; set; }
        public int? Year { get; set; }
        public string? DetailUrl { get; set; }
        public string? Description { get; set; }
    }

    public class ChatMessageDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Role { get; set; } = "user"; // "user" hoặc "model"
        public string Content { get; set; } = string.Empty;
        public List<ChatMovieCardDto> Movies { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ChatRequestDto
    {
        public string? ConversationId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? UserId { get; set; }
    }

    public class ChatResponseDto
    {
        public string ConversationId { get; set; } = string.Empty;
        public string Reply { get; set; } = string.Empty;
        public List<ChatMovieCardDto> Movies { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ChatHistoryDto
    {
        public string ConversationId { get; set; } = string.Empty;
        public List<ChatMessageDto> Messages { get; set; } = new();
    }
}
