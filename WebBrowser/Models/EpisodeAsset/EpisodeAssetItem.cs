using System.Text.Json.Serialization;

namespace WebBrowser.Models.EpisodeAsset
{
    public class EpisodeAssetItem
    {
        [JsonPropertyName("asseT_ID")]
        public int AssetId { get; set; }

        [JsonPropertyName("episodE_ID")]
        public int EpisodeId { get; set; }

        [JsonPropertyName("serieS_ID")]
        public int SeriesId { get; set; }

        [JsonPropertyName("seasoN_ID")]
        public int SeasonId { get; set; }

        [JsonPropertyName("episodE_NO")]
        public int EpisodeNo { get; set; }

        [JsonPropertyName("episodE_TITLE")]
        public string EpisodeTitle { get; set; }

        [JsonPropertyName("asseT_TYPE")]
        public string AssetType { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("sorT_ORDER")]
        public int SortOrder { get; set; }
    }
}
