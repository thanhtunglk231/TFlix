using Newtonsoft.Json;

namespace WebBrowser.Models.MovieAsset
{
    public class MovieAssetItem
    {
        [JsonProperty("assetId")]
        public decimal AssetId { get; set; }

        [JsonProperty("ownerType")]
        public string OwnerType { get; set; } = "MOVIE";

        [JsonProperty("ownerId")]
        public long OwnerId { get; set; }

        [JsonProperty("ownerTitle")]
        public string? OwnerTitle { get; set; }

        [JsonProperty("assetType")]
        public string? AssetType { get; set; }

        [JsonProperty("url")]
        public string? Url { get; set; }

        [JsonProperty("sortOrder")]
        public int? SortOrder { get; set; }
    }

    public class AssetOwnerItem
    {
        [JsonProperty("ownerId")]
        public long OwnerId { get; set; }
        [JsonProperty("ownerTitle")]
        public string OwnerTitle { get; set; } = string.Empty;

        [JsonProperty("nextOwnerType")]
        public string? NextOwnerType { get; set; }
        [JsonProperty("nextAssetId")]
        public long? NextAssetId { get; set; }
        [JsonProperty("hasMore")]
        public bool HasMore { get; set; }
    }
}
