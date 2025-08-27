using Newtonsoft.Json;

namespace WebBrowser.Models.Movie
{
    public class MovieTableWrapper
    {
        [JsonProperty("table")]
        public List<MovieItem> Table { get; set; } = new();
    }
}
