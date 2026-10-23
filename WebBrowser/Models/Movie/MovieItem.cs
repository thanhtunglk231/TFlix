using CommonLib.Helper;
using CommonLib.Hepler;
using Newtonsoft.Json;
using System.ComponentModel;

namespace WebBrowser.Models.Movie
{
    public class MovieItem
    {
        [JsonProperty("moviE_ID")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int MovieId { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("originaL_TITLE")]
        public string OriginalTitle { get; set; }

        [JsonProperty("releasE_DATE")]
        public DateTime? ReleaseDate { get; set; }

        [JsonProperty("duratioN_MIN")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? DurationMin { get; set; }

        [JsonProperty("countrY_CODE")]
        public string CountryCode { get; set; }

        [JsonProperty("languagE_CODE")]
        public string LanguageCode { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("iS_PREMIUM")]
        [JsonConverter(typeof(FlexibleBoolYnConverter))]
        public bool IsPremium { get; set; }

        [JsonProperty("createD_AT")]
        public DateTimeOffset? CreatedAt { get; set; }

        [JsonProperty("updateD_AT")]
        public DateTimeOffset? UpdatedAt { get; set; }

        [JsonProperty("posteR_URL")]
        public string PosterUrl { get; set; }

        [JsonProperty("genres")]
        public string Genres { get; set; }

        [JsonProperty("avG_RATING")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public decimal? AvgRating { get; set; }

        [JsonProperty("sourcE_COUNT")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int SourceCount { get; set; }

        [JsonProperty("serieS_ID")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? SeriesId { get; set; }

        [JsonProperty("serieS_TITLE")]
        public string? SeriesTitle { get; set; }

        [JsonProperty("seasoN_ID")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? SeasonId { get; set; }

        [JsonProperty("seasoN_NO")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? SeasonNo { get; set; }

        [JsonProperty("seasoN_TITLE")]
        public string? SeasonTitle { get; set; }

        [JsonProperty("episodE_NO")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? EpisodeNo { get; set; }

        [JsonProperty("episodE_ID")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? EpisodeId { get; set; }
    }
}
