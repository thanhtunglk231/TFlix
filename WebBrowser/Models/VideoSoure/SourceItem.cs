using CommonLib.Helper;
using CommonLib.Hepler;
using Newtonsoft.Json;

namespace WebBrowser.Models.VideoSoure
{
    public class SourceItem
    {
        [JsonProperty("sourcE_ID")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int SourceId { get; set; }

        [JsonProperty("moviE_ID")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? MovieId { get; set; }         // JSON có thể null

        [JsonProperty("episodE_ID")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? EpisodeId { get; set; }       // JSON có thể null (1.0)

        [JsonProperty("moviE_TITLE")]
        public string MovieTitle { get; set; }

        [JsonProperty("episodE_TITLE")]
        public string EpisodeTitle { get; set; }

        [JsonProperty("provider")]
        public string Provider { get; set; }

        [JsonProperty("serveR_NAME")]
        public string ServerName { get; set; }

        [JsonProperty("streaM_URL")]
        public string StreamUrl { get; set; }

        [JsonProperty("quality")]
        public string Quality { get; set; }

        [JsonProperty("format")]
        public string Format { get; set; }

        [JsonProperty("drM_TYPE")]
        public string DrmType { get; set; }

        [JsonProperty("drM_LICENSE_URL")]
        public string DrmLicenseUrl { get; set; }

        [JsonProperty("iS_PRIMARY")]
        [JsonConverter(typeof(FlexibleBoolYnConverter))]
        public bool IsPrimary { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("createD_AT")]
        public DateTimeOffset? CreatedAt { get; set; }
    }
}
