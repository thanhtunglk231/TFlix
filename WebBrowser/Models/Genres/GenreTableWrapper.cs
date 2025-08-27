using Newtonsoft.Json;
using WebBrowser.Models.EpisodeAsset;

namespace WebBrowser.Models.Genres
{
    public class GenreTableWrapper
    {

        [JsonProperty("table")]
        public List<GenreItem> Table { get; set; } = new();
    }
}
