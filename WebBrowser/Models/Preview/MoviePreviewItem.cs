using System.Text.Json;
using System.Text.Json.Serialization;
using WebBrowser.Models.Genres;
using WebBrowser.Models.MovieAsset;
using WebBrowser.Models.VideoSoure;

namespace WebBrowser.Models.Preview
{
    public class MoviePreviewItem
    {
        [JsonPropertyName("moviE_ID")] public int MovieId { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("originaL_TITLE")] public string? OriginalTitle { get; set; }
        [JsonPropertyName("overview")] public string? Overview { get; set; }
        [JsonPropertyName("releasE_DATE")] public DateTime? ReleaseDate { get; set; }
        [JsonPropertyName("duratioN_MIN")] public int? DurationMin { get; set; }
        [JsonPropertyName("agE_RATING")] public string? AgeRating { get; set; }
        [JsonPropertyName("countrY_CODE")] public string? CountryCode { get; set; }
        [JsonPropertyName("countrY_NAME")] public string? CountryName { get; set; }
        [JsonPropertyName("languagE_CODE")] public string? LanguageCode { get; set; }
        [JsonPropertyName("languagE_NAME")] public string? LanguageName { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("iS_PREMIUM")] public string? IsPremiumRaw { get; set; } // "Y"/"N"
        [JsonPropertyName("imdB_ID")] public string? ImdbId { get; set; }
        [JsonPropertyName("tmdB_ID")] public string? TmdbId { get; set; }
        [JsonPropertyName("createD_AT")] public DateTimeOffset? CreatedAt { get; set; }
        [JsonPropertyName("updateD_AT")] public DateTimeOffset? UpdatedAt { get; set; }

        // Poster/Backdrop/Trailer
        [JsonPropertyName("posteR_URL")] public string? PosterUrl { get; set; }
        [JsonPropertyName("backdroP_URL")] public string? BackdropUrl { get; set; }
        [JsonPropertyName("traileR_URL")] public string? TrailerUrl { get; set; }

        // Các cột JSON đang là CHUỖI
        [JsonPropertyName("genreS_JSON")] public string? GenresJson { get; set; }
        [JsonPropertyName("assetS_JSON")] public string? AssetsJson { get; set; }
        [JsonPropertyName("casT_JSON")] public string? CastJson { get; set; }
        [JsonPropertyName("creW_JSON")] public string? CrewJson { get; set; }
        [JsonPropertyName("sourceS_JSON")] public string? SourcesJson { get; set; }

        // Thống kê
        [JsonPropertyName("avG_RATING")] public decimal? AvgRating { get; set; }
        [JsonPropertyName("ratinG_COUNT")] public int? RatingCount { get; set; }
        [JsonPropertyName("commenT_COUNT")] public int? CommentCount { get; set; }

        /* ========== Thuộc tính tiện ích đã parse ========== */
        [JsonIgnore] public bool IsPremium => string.Equals(IsPremiumRaw, "Y", StringComparison.OrdinalIgnoreCase);
        [JsonIgnore] public List<GenreItem> Genres => ParseOrEmpty<List<GenreItem>>(GenresJson);
        [JsonIgnore] public List<MovieAssetItem> Assets => ParseOrEmpty<List<MovieAssetItem>>(AssetsJson);
        [JsonIgnore] public List<CastItem> Cast => ParseOrEmpty<List<CastItem>>(CastJson);
        [JsonIgnore] public List<CrewItem> Crew => ParseOrEmpty<List<CrewItem>>(CrewJson);
        //[JsonIgnore] public List<SourceItem> Sources => ParseSources(SourcesJson);

        private static readonly JsonSerializerOptions _opt = new() { PropertyNameCaseInsensitive = true };

        private static T ParseOrEmpty<T>(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return Activator.CreateInstance<T>();
            try { return JsonSerializer.Deserialize<T>(json, _opt) ?? Activator.CreateInstance<T>(); }
            catch { return Activator.CreateInstance<T>(); }
        }

        // sources_json có field "subtitles" là CHUỖI JSON → parse lồng
        //private static List<SourceItem> ParseSources(string? json)
        //{
        //    var list = ParseOrEmpty<List<SourceItem>>(json);
        //    foreach (var s in list)
        //    {
        //        s.Subtitles = ParseOrEmpty<List<SubtitleItem>>(s.SubtitlesJson);
        //    }
        //    return list;
        //}

        public sealed class CastItem
        {
            [JsonPropertyName("person_id")] public int PersonId { get; set; }
            [JsonPropertyName("name")] public string? Name { get; set; }
            [JsonPropertyName("character")] public string? Character { get; set; }
            [JsonPropertyName("order")] public int? Order { get; set; }
        }

        public sealed class CrewItem
        {
            [JsonPropertyName("person_id")] public int PersonId { get; set; }
            [JsonPropertyName("name")] public string? Name { get; set; }
            [JsonPropertyName("job")] public string? Job { get; set; }     // DIRECTOR/WRITER/...
        }
        public sealed class SubtitleItem
        {
            [JsonPropertyName("subtitle_id")] public int SubtitleId { get; set; }
            [JsonPropertyName("language_code")] public string? LanguageCode { get; set; }
            [JsonPropertyName("format")] public string? Format { get; set; } // VTT/SRT
            [JsonPropertyName("url")] public string? Url { get; set; }
            [JsonPropertyName("is_default")] public string? IsDefaultRaw { get; set; }   // "Y"/"N"
            [JsonIgnore] public bool IsDefault => string.Equals(IsDefaultRaw, "Y", StringComparison.OrdinalIgnoreCase);
        }
    }
}
