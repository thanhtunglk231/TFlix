using CoreLib.Dtos.Series;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SeriesController : Controller
    {
        private readonly ISeriesService _seriesService;
        public SeriesController(ISeriesService seriesService) => _seriesService = seriesService;

        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            Console.WriteLine("[Admin/SeriesController] -> GetAll() ENTER");
            var result = await _seriesService.get_all();
            Console.WriteLine("[Admin/SeriesController] <- GetAll() EXIT: " + JsonConvert.SerializeObject(result));
            return Ok(result);
        }

        // ====== ADD ======
        // Nhận payload linh hoạt: IsPremium có thể "Y"/"N" hoặc "true"/"false"
        [HttpPost]
        public async Task<IActionResult> AddSeries([FromBody] AddSeriesDto dto)
        {
            Console.WriteLine("[Admin/SeriesController] <- AddSeries() Calling: ");

            var result = await _seriesService.add_series(dto);
            Console.WriteLine("[Admin/SeriesController] <- AddSeries() EXIT: " + JsonConvert.SerializeObject(result));
            return Ok(result);
        }

        // ====== UPDATE ======
        [HttpPost]
        public async Task<IActionResult> UpdateSeries([FromBody] UpdateSeriesDto dto)
        {
            Console.WriteLine("[Admin/SeriesController] <- UpdateSeries() Calling: ");
            var result = await _seriesService.uppdate_series(dto);
            return Ok(result);
        }

        // ====== DELETE ======
        [HttpGet]
        public async Task<IActionResult> Delete([FromQuery] decimal id)
        {
            Console.WriteLine("[Admin/SeriesController] -> Delete() ENTER, id=" + id);
            if (id <= 0) return BadRequest(new { message = "Invalid id." });

            var result = await _seriesService.delete_series(id);
            Console.WriteLine("[Admin/SeriesController] <- Delete() EXIT: " + JsonConvert.SerializeObject(result));
            return Ok(result);
        }

        // ====== Helpers ======
        private static bool ParseBoolFromYN(string? val)
        {
            if (string.IsNullOrWhiteSpace(val)) return false;
            var s = val.Trim().ToUpperInvariant();
            if (s == "Y") return true;
            if (s == "N") return false;
            if (bool.TryParse(val, out var b)) return b; // chấp nhận "true"/"false"
            return false;
        }

        private static AddSeriesDto MapToAddSeriesDto(AddSeriesClientModel dto)
        {
            return new AddSeriesDto
            {
                Title = dto.Title?.Trim() ?? string.Empty,
                OriginalTitle = string.IsNullOrWhiteSpace(dto.OriginalTitle) ? null : dto.OriginalTitle!.Trim(),
                Overview = string.IsNullOrWhiteSpace(dto.Overview) ? null : dto.Overview!.Trim(),
                FirstAirDate = dto.FirstAirDate,
                LastAirDate = dto.LastAirDate,
                CountryCode = string.IsNullOrWhiteSpace(dto.CountryCode) ? null : dto.CountryCode!.Trim(),
                LanguageCode = string.IsNullOrWhiteSpace(dto.LanguageCode) ? null : dto.LanguageCode!.Trim(),
                Status = string.IsNullOrWhiteSpace(dto.Status) ? null : dto.Status!.Trim(),
                IsPremium = ParseBoolFromYN(dto.IsPremium),
                ImdbId = string.IsNullOrWhiteSpace(dto.ImdbId) ? null : dto.ImdbId!.Trim(),
                TmdbId = string.IsNullOrWhiteSpace(dto.TmdbId) ? null : dto.TmdbId!.Trim()
            };
        }
    }

    // ====== Client models (KHÔNG có PosterUrl/Genres) ======
    public class AddSeriesClientModel
    {
        public string Title { get; set; } = string.Empty;
        public string? OriginalTitle { get; set; }
        public string? Overview { get; set; }
        public DateTime? FirstAirDate { get; set; }
        public DateTime? LastAirDate { get; set; }
        public string? CountryCode { get; set; }
        public string? LanguageCode { get; set; }
        public string? Status { get; set; }
        public string IsPremium { get; set; } = "N"; // "Y"/"N" hoặc "true"/"false"
        public string? ImdbId { get; set; }
        public string? TmdbId { get; set; }
    }

    public class UpdateSeriesClientModel : AddSeriesClientModel
    {
        public decimal SeriesId { get; set; }
    }
}
