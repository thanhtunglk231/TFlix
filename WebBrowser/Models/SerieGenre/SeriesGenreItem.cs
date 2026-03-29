using Newtonsoft.Json;

namespace WebBrowser.Models.SerieGenre
{
    public class SeriesGenreItem
    {
        [JsonProperty("genre_id")]
        public decimal GenreId { get; set; }

        [JsonProperty("genre_name")]
        public string GenreName { get; set; } = string.Empty;

        [JsonProperty("slug")]
        public string Slug { get; set; } = string.Empty;
    }
}