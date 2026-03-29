using CoreLib.Dtos.SeriesGenre;
using Microsoft.AspNetCore.Mvc;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SeriesGenreController : Controller
    {
        private readonly ISerireGenreService _seriesGenreService;

        public SeriesGenreController(ISerireGenreService seriesGenreService)
        {
            _seriesGenreService = seriesGenreService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetBySeries(decimal id)
        {
            var result = await _seriesGenreService.get_byid(id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AddSeriesGenreDto dto)
        {
            var result = await _seriesGenreService.add_MovieGenre(dto);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody] UpdateSeriesGenreDto dto)
        {
            var result = await _seriesGenreService.uppdate_MovieGenre(dto);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] DeleteSeriesGenreDto dto)
        {
            var result = await _seriesGenreService.delete_Episode(dto);
            return Ok(result);
        }
    }
}