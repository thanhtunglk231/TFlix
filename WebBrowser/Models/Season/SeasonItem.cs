using CommonLib.Helper;
using Newtonsoft.Json;

namespace WebBrowser.Models.Season
{
    public class SeasonItem
    {
        [JsonProperty("seasoN_ID")]
        public decimal SeasonId { get; set; }

        [JsonProperty("serieS_ID")]
        public decimal SeriesId { get; set; }

        [JsonProperty("seasoN_NO")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int SeasonNo { get; set; }

        [JsonProperty("title")]
        public string? Title { get; set; }

        [JsonProperty("overview")]
        public string? Overview { get; set; }

        [JsonProperty("aiR_DATE")]
        public DateTime? AirDate { get; set; }

        [JsonProperty("posteR_URL")]
        public string? PosterUrl { get; set; }

        [JsonProperty("episodE_COUNT")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int EpisodeCount { get; set; }

        [JsonProperty("totaL_DURATION_MIN")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? TotalDurationMin { get; set; }

        [JsonProperty("avG_RATING")]
        public decimal? AvgRating { get; set; }

        [JsonProperty("sourcE_COUNT")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int SourceCount { get; set; }

        [JsonProperty("latesT_AIR_DATE")]
        public DateTime? LatestAirDate { get; set; }

        [JsonProperty("serieS_TITLE")]
        public string? SeriesTitle { get; set; }
    }
}
