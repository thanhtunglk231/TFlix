using System;

namespace CoreLib.Dtos.Movies
{
    public class MovieCatalogItemDto
    {
        public long MovieId { get; set; }
        public string Title { get; set; } = default!;
        public string? OriginalTitle { get; set; }
        public string? Overview { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public int? DurationMin { get; set; }
        public string? CountryCode { get; set; }
        public string? LanguageCode { get; set; }
        public string? Status { get; set; }
        public string IsPremiumYN { get; set; } = "N";
        public bool IsPremium => string.Equals(IsPremiumYN, "Y", StringComparison.OrdinalIgnoreCase);
        public DateTime CreatedAt { get; set; }
        public string? PosterUrl { get; set; }
        public string? Genres { get; set; }
    }
}
