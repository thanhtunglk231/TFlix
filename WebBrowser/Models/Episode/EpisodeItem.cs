using CommonLib.Helper;
using CommonLib.Hepler;
using Newtonsoft.Json;
using System.ComponentModel;

namespace WebBrowser.Models.Episode
{
    public class EpisodeItem
    {
        [JsonProperty("episode_id")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public long EpisodeId { get; set; }

        [JsonProperty("series_id")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public long SeriesId { get; set; }

        // Extra display fields
        [JsonProperty("series_title")]
        public string SeriesTitle { get; set; }

        // Season / Episode numbers
        [JsonProperty("season_id")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public long SeasonId { get; set; }

        [JsonProperty("season_no")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int SeasonNo { get; set; }

        [JsonProperty("episode_no")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int EpisodeNo { get; set; }

        [JsonProperty("episode_title")]
        public string EpisodeTitle { get; set; }

        [JsonProperty("overview")]
        public string? Overview { get; set; }

        // Dates & duration
        [JsonProperty("air_date")]
        public DateTime? AirDate { get; set; }

        [JsonProperty("duration_min")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? DurationMin { get; set; }

        // Status & flags
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("is_premium")]
        [JsonConverter(typeof(FlexibleBoolYnConverter))] // "Y"/"N" -> bool
        public bool IsPremium { get; set; }

        // Media & metrics
        [JsonProperty("cover_url")]
        public string CoverUrl { get; set; }

        [JsonProperty("avg_rating")]
     
        public decimal? AvgRating { get; set; }

        [JsonProperty("source_count")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int SourceCount { get; set; }
    }
}
