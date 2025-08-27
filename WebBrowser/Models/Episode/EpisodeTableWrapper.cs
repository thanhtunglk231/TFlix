using Newtonsoft.Json;
using WebBrowser.Models.Movie;

namespace WebBrowser.Models.Episode
{
    public class EpisodeTableWrapper
    {
        [JsonProperty("table")]
        public List<EpisodeItem> Table { get; set; } = new();
    }
}
