using Newtonsoft.Json;

namespace WebBrowser.Models.MovieGenre
{
    public class MovieGenreItem
    {
        [JsonProperty("genrE_ID")]
        public decimal GenreId { get; set; }

        [JsonProperty("genrE_NAME")]
        public string GenreName { get; set; } = string.Empty;
        [JsonProperty("slug")]
        public string Slug { get; set; } = string.Empty;
    }
}
