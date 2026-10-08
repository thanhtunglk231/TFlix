using Newtonsoft.Json;
using WebBrowser.Models.Movie;

namespace WebBrowser.Models.MovieAsset
{
    public class MovieAssetTableWrapper
    {
        [JsonProperty("table")]
        public List<MovieAssetItem> Table { get; set; } = new();
        [JsonProperty("table1")]
        public List<AssetOwnerItem> Table1 { get; set; } = new();
        [JsonProperty("table2")]
        public List<AssetOwnerItem> Table2 { get; set; } = new();
    }
}
