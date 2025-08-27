using Newtonsoft.Json;
using WebBrowser.Models.Movie;

namespace WebBrowser.Models.MovieAsset
{
    public class MovieAssetTableWrapper
    {
        [JsonProperty("table")]
        public List<MovieAssetItem> Table { get; set; } = new();
    }
}
