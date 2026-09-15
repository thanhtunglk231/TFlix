using Newtonsoft.Json;
using System;

namespace WebBrowser.Models.Preview
{
    public class PreviewItem
    {
        [JsonProperty("kind")]
        public string? kind { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("movieId")]
        public long MovieId { get; set; }

        [JsonProperty("contenT_ID")]
        public double ContentId { get; set; }

        [JsonProperty("title")]
        public string? title { get; set; }

        [JsonProperty("originalTitle")]
        public string? OriginalTitle { get; set; }

        [JsonProperty("overviewText")]
        public string? OverviewText { get; set; }

        [JsonProperty("releaseOrAirDate")]
        public DateTime? ReleaseOrAirDate { get; set; }

        [JsonProperty("durationMin")]
        public double? DurationMin { get; set; }

        [JsonProperty("ageRating")]
        public string? AgeRating { get; set; }

        [JsonProperty("countryCode")]
        public string? CountryCode { get; set; }

        [JsonProperty("countryName")]
        public string? CountryName { get; set; }

        [JsonProperty("languageCode")]
        public string? LanguageCode { get; set; }

        [JsonProperty("languageName")]
        public string? LanguageName { get; set; }

        [JsonProperty("status")]
        public string? status { get; set; }

        [JsonProperty("isPremiumYN")]
        public string? IsPremium { get; set; }

        [JsonProperty("imdbId")]
        public string? ImdbId { get; set; }

        [JsonProperty("tmdbId")]
        public string? TmdbId { get; set; }

        [JsonProperty("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonProperty("genres")]
        public string? genres { get; set; }

        [JsonProperty("primaryStreamUrl")]
        public string? PrimaryStreamUrl { get; set; }

        [JsonProperty("primaryStreamQuality")]
        public string? PrimaryStreamQuality { get; set; }

        [JsonProperty("primaryStreamFormat")]
        public string? PrimaryStreamFormat { get; set; }

        [JsonProperty("primaryPosterUrl")]
        public string? PrimaryPosterUrl { get; set; }

        [JsonProperty("primaryBackdropUrl")]
        public string? PrimaryBackdropUrl { get; set; }

        [JsonProperty("primaryThumbUrl")]
        public string? PrimaryThumbUrl { get; set; }
    }
}
