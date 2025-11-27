using Newtonsoft.Json;

namespace WebBrowser.Models.Preview
{
    public class PreviewItem
    {
        public string kind { get; set; }

        // API trả 44.0 -> phải dùng double
        [JsonProperty("contenT_ID")]
        public double ContentId { get; set; }

        public string title { get; set; }

        [JsonProperty("originaL_TITLE")]
        public string OriginalTitle { get; set; }

        [JsonProperty("overvieW_TEXT")]
        public string OverviewText { get; set; }

        [JsonProperty("releasE_OR_AIR_DATE")]
        public DateTime? ReleaseOrAirDate { get; set; }

        // API có thể trả 6.0 -> dùng double? an toàn hơn
        [JsonProperty("duratioN_MIN")]
        public double? DurationMin { get; set; }

        [JsonProperty("agE_RATING")]
        public string AgeRating { get; set; }

        [JsonProperty("countrY_CODE")]
        public string CountryCode { get; set; }

        [JsonProperty("countrY_NAME")]
        public string CountryName { get; set; }

        [JsonProperty("languagE_CODE")]
        public string LanguageCode { get; set; }

        [JsonProperty("languagE_NAME")]
        public string LanguageName { get; set; }

        public string status { get; set; }

        // "Y" / "N" -> string là đúng
        [JsonProperty("iS_PREMIUM")]
        public string IsPremium { get; set; }

        [JsonProperty("imdB_ID")]
        public string ImdbId { get; set; }

        [JsonProperty("tmdB_ID")]
        public string TmdbId { get; set; }

        [JsonProperty("createD_AT")]
        public DateTime? CreatedAt { get; set; }

        [JsonProperty("updateD_AT")]
        public DateTime? UpdatedAt { get; set; }

        public string genres { get; set; }

        [JsonProperty("primarY_STREAM_URL")]
        public string PrimaryStreamUrl { get; set; }

        [JsonProperty("primarY_STREAM_QUALITY")]
        public string PrimaryStreamQuality { get; set; }

        [JsonProperty("primarY_STREAM_FORMAT")]
        public string PrimaryStreamFormat { get; set; }

        [JsonProperty("primarY_POSTER_URL")]
        public string PrimaryPosterUrl { get; set; }

        [JsonProperty("primarY_BACKDROP_URL")]
        public string PrimaryBackdropUrl { get; set; }

        [JsonProperty("primarY_THUMB_URL")]
        public string PrimaryThumbUrl { get; set; }
    }
}
