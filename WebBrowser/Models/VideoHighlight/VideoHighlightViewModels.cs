using CoreLib.Dtos.VideoHighlight;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace WebBrowser.Models.VideoHighlight;

public sealed class VideoHighlightTableWrapper
{
    [JsonProperty("table")]
    public List<VideoHighlightDto> Table { get; set; } = new();
    [JsonProperty("table1")]
    [JsonPropertyName("table1")]
    public List<VideoHighlightPagination> Pagination { get; set; } = new();
}
public sealed class VideoHighlightPagination
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
