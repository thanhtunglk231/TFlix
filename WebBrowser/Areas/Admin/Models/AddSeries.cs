using System.ComponentModel.DataAnnotations;

namespace WebBrowser.Areas.Admin.Models
{
    public class AddSeries
    {
        [Required(ErrorMessage = "The Title field is required.")]
        public string Title { get; set; } = string.Empty;

        public string? OriginalTitle { get; set; }
        public string? Overview { get; set; }

        public DateTime? FirstAirDate { get; set; }
        public DateTime? LastAirDate { get; set; }

        public string? CountryCode { get; set; }
        public string? LanguageCode { get; set; }

        public string? Status { get; set; }

        // ✅ để boolean, tránh lỗi convert
        public string IsPremium { get; set; } = "N";


        public string? ImdbId { get; set; }
        public string? TmdbId { get; set; }

        public string? PosterUrl { get; set; }

        // Nếu muốn nhiều thể loại => để List<string>
        public string? Genres { get; set; }
    }
}
