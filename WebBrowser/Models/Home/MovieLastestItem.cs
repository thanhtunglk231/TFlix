using CommonLib.Helper;
using Newtonsoft.Json;

namespace WebBrowser.Models.Home
{
    public class MovieLastestItem
    {
        [JsonProperty("moviE_ID")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int MovieId { get; set; }       // nếu API trả 44.0, có thể gắn FlexibleIntConverter
        [JsonProperty("kind")]
        public string Kind { get; set; }
        [JsonProperty("title")]
        public string? Title { get; set; }

        [JsonProperty("originaL_TITLE")]
        public string? OriginalTitle { get; set; }

        [JsonProperty("releasE_DATE")]
        public DateTime? ReleaseDate { get; set; }

        [JsonProperty("status")]
        public string? Status { get; set; }

        // RAW: nhận 'Y' / 'N' từ API (đổi tên để khỏi trùng)
        [JsonProperty("iS_PREMIUM")]
        public string? IsPremiumRaw { get; set; }

        // BOOL: dùng trong code cho tiện
        [JsonIgnore]
        public bool IsPremium =>
            string.Equals(IsPremiumRaw, "Y", StringComparison.OrdinalIgnoreCase);

        [JsonProperty("posteR_URL")]
        public string? PosterUrl { get; set; }

        [JsonProperty("backdroP_URL")]
        public string? BackdropUrl { get; set; }

        [JsonProperty("sourcE_COUNT")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int SourceCount { get; set; }   // nếu API trả 1.0 → có thể dùng FlexibleIntConverter

        [JsonProperty("genres")]
        public string? Genres { get; set; }
    }
}
