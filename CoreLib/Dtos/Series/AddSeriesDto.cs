using System;
using System.ComponentModel.DataAnnotations;

namespace CoreLib.Dtos.Series
{
    public class AddSeriesDto
    {
        public string Title { get; set; } = default!;

        public string? OriginalTitle { get; set; }

        public string? Overview { get; set; }

        public DateTime? FirstAirDate { get; set; }

        public DateTime? LastAirDate { get; set; }

        /// <summary>
        /// Mã quốc gia (VD: VN, US, JP)
        /// </summary>
        public string? CountryCode { get; set; }

        /// <summary>
        /// Mã ngôn ngữ (VD: vi, en, ja)
        /// </summary>
        public string? LanguageCode { get; set; }

        /// <summary>
        /// ONGOING (default) | DRAFT | ENDED | PUBLISHED | ARCHIVED
        /// </summary>
        public string Status { get; set; } = "ONGOING";

        public bool IsPremium { get; set; } = false;

        public string? ImdbId { get; set; }

        public string? TmdbId { get; set; }
    }

    // DTO để update Series (kế thừa AddSeriesDto, bổ sung Id)
    public class UpdateSeriesDto : AddSeriesDto
    {
        public decimal SeriesId { get; set; }
    }
}
