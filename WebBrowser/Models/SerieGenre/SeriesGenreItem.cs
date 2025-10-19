using Newtonsoft.Json;

namespace WebBrowser.Models.SerieGenre
{
    public class SeriesGenreItem
    {
        [JsonProperty("genrE_ID")]
        public decimal GenreId { get; set; }

        [JsonProperty("genrE_NAME")]
        public string GenreName { get; set; } = string.Empty;
        [JsonProperty("slug")]
        public string Slug { get; set; } = string.Empty;
    }
}
