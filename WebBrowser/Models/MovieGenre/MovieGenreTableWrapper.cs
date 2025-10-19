using Newtonsoft.Json;
using WebBrowser.Models.MovieAsset;

namespace WebBrowser.Models.MovieGenre
{
    public class MovieGenreTableWrapper
    {
        [JsonProperty("table")]
        public List<MovieGenreItem> Table { get; set; } = new();
    }
}
