using CommonLib.Helper;              // <-- đúng namespace
using Newtonsoft.Json;

namespace WebBrowser.Models.Series
{
    public class SerieDto
    {
        [JsonProperty("serieS_ID")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int SeriesId { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("originaL_TITLE")]
        public string OriginalTitle { get; set; }

        [JsonProperty("firsT_AIR_DATE")]
        public DateTime? FirstAirDate { get; set; }

        [JsonProperty("lasT_AIR_DATE")]
        public DateTime? LastAirDate { get; set; }

        [JsonProperty("countrY_CODE")]
        public string CountryCode { get; set; }

        [JsonProperty("languagE_CODE")]
        public string LanguageCode { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("iS_PREMIUM")]
        public string IsPremium { get; set; } // "Y"/"N"

        [JsonProperty("imdB_ID")]
        public string ImdbId { get; set; }

        [JsonProperty("tmdB_ID")]
        public string TmdbId { get; set; }

        [JsonProperty("createD_AT")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updateD_AT")]
        public DateTime? UpdatedAt { get; set; }

        [JsonProperty("posteR_URL")]
        public string PosterUrl { get; set; }

        [JsonProperty("genres")]
        public string Genres { get; set; }

        [JsonProperty("seasoN_COUNT")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int SeasonCount { get; set; }

        [JsonProperty("episodE_COUNT")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int EpisodeCount { get; set; }

        [JsonProperty("avG_RATING")]
        public double? AvgRating { get; set; }

        [JsonProperty("sourcE_COUNT")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int SourceCount { get; set; }

        [JsonProperty("latesT_AIR_DATE")]
        public DateTime? LatestAirDate { get; set; }
    }
}
