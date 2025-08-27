using CommonLib.Helper;
using CommonLib.Hepler;
using Newtonsoft.Json;
using System.ComponentModel;

namespace WebBrowser.Models.Episode
{
    public class EpisodeItem
    {
        [JsonProperty("episodE_ID")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int EpisodeId { get; set; }

        [JsonProperty("serieS_ID")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int SeriesId { get; set; }

        // Extra display fields
        [JsonProperty("serieS_TITLE")]
        public string SeriesTitle { get; set; }

        // Season / Episode numbers
        [JsonProperty("seasoN_ID")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int SeasonId { get; set; }

        [JsonProperty("seasoN_NO")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int SeasonNo { get; set; }

        [JsonProperty("episodE_NO")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int EpisodeNo { get; set; }

        [JsonProperty("episodE_TITLE")]
        public string EpisodeTitle { get; set; }

        // Dates & duration
        [JsonProperty("aiR_DATE")]
        public DateTime? AirDate { get; set; }

        [JsonProperty("duratioN_MIN")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? DurationMin { get; set; }

        // Status & flags
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("iS_PREMIUM")]
        [JsonConverter(typeof(FlexibleBoolYnConverter))] // "Y"/"N" -> bool
        public bool IsPremium { get; set; }

        // Media & metrics
        [JsonProperty("coveR_URL")]
        public string CoverUrl { get; set; }

        [JsonProperty("avG_RATING")]
     
        public decimal? AvgRating { get; set; }

        [JsonProperty("sourcE_COUNT")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int SourceCount { get; set; }
    }
}
