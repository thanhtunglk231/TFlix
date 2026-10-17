using System.Text.Json.Serialization;

namespace CoreLib.Dtos.VideoHighlight;

public sealed class VideoHighlightDto
{
    public long HighlightId { get; set; }
    public long SourceId { get; set; }
    public long? MovieId { get; set; }
    public long? EpisodeId { get; set; }
    public string? MovieTitle { get; set; }
    public string? EpisodeTitle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal StartSeconds { get; set; }
    public decimal EndSeconds { get; set; }
    public string VideoUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int SortOrder { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public DateTimeOffset? CreatedAt { get; set; }
}

public sealed class CreateVideoHighlightDto
{
    public long SourceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal StartSeconds { get; set; }
    public decimal EndSeconds { get; set; }
    public string VideoUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int SortOrder { get; set; }
}

public sealed class GeminiHighlightSuggestion
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    [JsonPropertyName("startSeconds")]
    public double StartSeconds { get; set; }
    [JsonPropertyName("endSeconds")]
    public double EndSeconds { get; set; }
}
