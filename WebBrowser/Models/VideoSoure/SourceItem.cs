using CommonLib.Helper;
using CommonLib.Hepler;
using Newtonsoft.Json;

namespace WebBrowser.Models.VideoSoure
{
    public class SourceItem
    {
        [JsonProperty("source_id")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int SourceId { get; set; }

        [JsonProperty("movie_id")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public long? MovieId { get; set; }         // JSON có thể null

        [JsonProperty("episode_id")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public long? EpisodeId { get; set; }       // JSON có thể null (1.0)

        [JsonProperty("movie_title")]
        public string? MovieTitle { get; set; }

        [JsonProperty("episode_title")]
        public string? EpisodeTitle { get; set; }

        [JsonProperty("provider")]
        public string? Provider { get; set; }

        [JsonProperty("server_name")]
        public string? ServerName { get; set; }

        [JsonProperty("stream_url")]
        public string? StreamUrl { get; set; }

        [JsonProperty("quality")]
        public string? Quality { get; set; }

        [JsonProperty("format")]
        public string? Format { get; set; }

        [JsonProperty("drm_type")]
        public string? DrmType { get; set; }

        [JsonProperty("drm_license_url")]
        public string? DrmLicenseUrl { get; set; }

        [JsonProperty("is_primary")]
        [JsonConverter(typeof(FlexibleBoolYnConverter))]
        public bool IsPrimary { get; set; }

        [JsonProperty("status")]
        public string? Status { get; set; }

        [JsonProperty("created_at")]
        public DateTimeOffset? CreatedAt { get; set; }
    }
}
