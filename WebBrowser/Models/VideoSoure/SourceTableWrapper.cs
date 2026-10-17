using Newtonsoft.Json;

using System.Text.Json.Serialization;

namespace WebBrowser.Models.VideoSoure
{
    public class SourceTableWrapper
    {
        [JsonProperty("table")]
        public List<SourceItem> Table { get; set; } = new();

        [JsonProperty("table1")]
        [JsonPropertyName("table1")]
        public List<SourcePaginationItem> Pagination { get; set; } = new();
    }

    public class SourcePaginationItem
    {
        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }

        [JsonProperty("page")]
        public int Page { get; set; }

        [JsonProperty("pageSize")]
        public int PageSize { get; set; }

        [JsonProperty("totalPages")]
        public int TotalPages { get; set; }
    }
}
