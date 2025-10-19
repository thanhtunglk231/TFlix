using CoreLib.Dtos.SeriesGenre;
using Microsoft.AspNetCore.Mvc;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SeriesGenreController : Controller
    {
        private readonly ISerireGenreService _movieGenreService;
        public SeriesGenreController(ISerireGenreService movieGenreService)
        {
            _movieGenreService = movieGenreService;
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> GetByMovie(decimal id)
        {
            var result = await _movieGenreService.get_byid(id);
            return Ok(result);
        }
        public async Task<IActionResult> Add(AddSeriesGenreDto addMovieGenreDto)
        {
            var result = await _movieGenreService.add_MovieGenre(addMovieGenreDto);
            return Ok(result);
        }

        public async Task<IActionResult> Update(UpdateSeriesGenreDto updateMovieGenreDto)
        {
            var result = await _movieGenreService.uppdate_MovieGenre(updateMovieGenreDto);
            return Ok(result);
        }
        public async Task<IActionResult> Delete(DeleteSeriesGenreDto deleteMovieGenreDto)
        {
            var result = await _movieGenreService.delete_Episode(deleteMovieGenreDto);
            return Ok(result);

        }
    }
}
