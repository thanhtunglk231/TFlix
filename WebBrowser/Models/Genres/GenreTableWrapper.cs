using Newtonsoft.Json;
using WebBrowser.Models.EpisodeAsset;

namespace WebBrowser.Models.Genres
{
    public class GenreTableWrapper
    {

        [JsonProperty("table")]
        public List<GenreItem> Table { get; set; } = new();

        [JsonProperty("table1")]
        public List<GenrePageInfo> PageInfo { get; set; } = new();
    }

    public class GenrePageInfo
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
