using Newtonsoft.Json;
using WebBrowser.Models.Episode;

namespace WebBrowser.Models.EpisodeAsset
{
    public class EpisodeAssetTableWrapper
    {
        [JsonProperty("table")]
        public List<EpisodeAssetItem> Table { get; set; } = new();
    }
}
