using System.ComponentModel.DataAnnotations;

namespace CoreLib.Dtos.Genres
{
    public class AddGenreDto
    {
        [Required(ErrorMessage = "genreName là bắt buộc")]
        [StringLength(100, ErrorMessage = "genreName tối đa 100 ký tự")]
        public string genreName { get; set; } = default!;

        // Tuỳ chọn. Nếu không gửi, SP sẽ tự tạo slug
        [StringLength(120, ErrorMessage = "slug tối đa 120 ký tự")]
        public string? slug { get; set; }
    }

    public class UpdateGenreDto : AddGenreDto
    {
        [Required]
        public decimal GenreId { get; set; }
    }
}
