using Newtonsoft.Json;

namespace WebBrowser.Models.Home
{
    public class EpisodeLatestTableWrapper
    {
        [JsonProperty("table")]
        public List<EpisodeLatestItem> Table { get; set; } = new();
    }

    public class EpisodeLatestItem
    {
        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("episode_id")]
        public int EpisodeId { get; set; }

        [JsonProperty("series_id")]
        public int SeriesId { get; set; }

        [JsonProperty("series_title")]
        public string SeriesTitle { get; set; }

        [JsonProperty("season_no")]
        public int SeasonNo { get; set; }

        [JsonProperty("episode_no")]
        public int EpisodeNo { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("air_date")]
        public DateTime? AirDate { get; set; }

        [JsonProperty("duration_min")]
        public int? DurationMin { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("is_premium")]
        public string IsPremium { get; set; }

        [JsonProperty("thumb_url")]
        public string ThumbUrl { get; set; }

        [JsonProperty("still_url")]
        public string StillUrl { get; set; }

        [JsonProperty("trailer_url")]
        public string TrailerUrl { get; set; }

        [JsonProperty("source_count")]
        public int SourceCount { get; set; }

        [JsonProperty("genres")]
        public string Genres { get; set; }
    }
}
