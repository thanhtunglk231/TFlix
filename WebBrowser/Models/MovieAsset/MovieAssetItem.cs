using Newtonsoft.Json;

namespace WebBrowser.Models.MovieAsset
{
    public class MovieAssetItem
    {
        [JsonProperty("asseT_ID")]
        public decimal AssetId { get; set; }

        [JsonProperty("moviE_ID")]
        public decimal MovieId { get; set; }

        [JsonProperty("moviE_TITLE")]
        public string? MovieTitle { get; set; }

        [JsonProperty("asseT_TYPE")]
        public string? AssetType { get; set; }

        [JsonProperty("url")]
        public string? Url { get; set; }

        [JsonProperty("sorT_ORDER")]
        public int? SortOrder { get; set; }
    }
}
