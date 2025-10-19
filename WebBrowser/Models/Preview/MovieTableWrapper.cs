using Newtonsoft.Json;
using WebBrowser.Models.MovieGenre;

namespace WebBrowser.Models.Preview
{
    public class MovieTableWrapper
    {
        [JsonProperty("table")]
        public List<MoviePreviewItem> Table { get; set; } = new();
    }
}
