// CoreLib/Dtos/Episode/AddEpisodeDto.cs
using System;

namespace CoreLib.Dtos.Episode
{
    public class AddEpisodeDto
    {
        public decimal SeriesId { get; set; }
        public decimal SeasonId { get; set; }
        public int EpisodeNo { get; set; }

        public string Title { get; set; } = default!;
        public string? Overview { get; set; }

        public DateTime? AirDate { get; set; }
        public int? DurationMin { get; set; }

        public string Status { get; set; } = "PUBLISHED";

        // DB: 'Y'/'N' – map sang bool
        public string IsPremiumYN { get; set; } = "N";
        public bool IsPremium
        {
            get => string.Equals(IsPremiumYN, "Y", StringComparison.OrdinalIgnoreCase);
            set => IsPremiumYN = value ? "Y" : "N";
        }

        // Cho phép null để không gây 400 nếu client không gửi
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class UpdateEpisodeDto : AddEpisodeDto
    {
        public decimal EpisodeId { get; set; }
    }
}
