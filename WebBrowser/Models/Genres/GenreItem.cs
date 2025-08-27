using CommonLib.Helper;
using Newtonsoft.Json;

namespace WebBrowser.Models.Genres
{
    public class GenreItem
    {

        [JsonProperty("genrE_ID")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? GenreId { get; set; }

        [JsonProperty("genrE_NAME")]
        public string? GenreName { get; set; }

        [JsonProperty("slug")]
        public string? Slug { get; set; }

        [JsonProperty("moviE_COUNT")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? MovieCount { get; set; }

        [JsonProperty("serieS_COUNT")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? SeriesCount { get; set; }

    }
}
