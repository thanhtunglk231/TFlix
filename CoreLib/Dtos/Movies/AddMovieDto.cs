using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Dtos.Movies
{
    public class AddMovieDto
    {
        // NUMBER -> decimal
        public string Title { get; set; } = default!;
        public string? OriginalTitle { get; set; }
        public string? Overview { get; set; }         // CLOB -> string
        public DateTime? ReleaseDate { get; set; }    // DATE
        public int? DurationMin { get; set; }         // NUMBER
        public string? AgeRating { get; set; }
        public string? CountryCode { get; set; }
        public string? LanguageCode { get; set; }
        public string Status { get; set; } = "PUBLISHED";
        public string IsPremiumYN { get; set; } = "N"; // CHAR(1) 'Y'/'N' trong DB

        // Tiện cho code C#: map Y/N <-> bool
        public bool IsPremium
        {
            get => string.Equals(IsPremiumYN, "Y", StringComparison.OrdinalIgnoreCase);
            set => IsPremiumYN = value ? "Y" : "N";
        }

        public string? ImdbId { get; set; }
        public string? TmdbId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class UpdateMovieDto : AddMovieDto
    {

        public decimal Movideid { get; set; }
    }
}

