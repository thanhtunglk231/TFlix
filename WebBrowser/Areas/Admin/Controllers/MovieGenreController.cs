using Microsoft.AspNetCore.Mvc;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Areas.Admin.Controllers
{
    [Area("Admin")]
  
    public class MovieGenreController : Controller
    {
        private readonly IMovieGenreService _movieGenreService;
        public MovieGenreController(IMovieGenreService movieGenreService)
        {
            _movieGenreService = movieGenreService;
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> GetByMovie([FromQuery]decimal id)
        {
            var result = await _movieGenreService.get_byid(id);
            return Ok(result);
        }
        public async Task<IActionResult> Add([FromBody]CoreLib.Dtos.MovieGenre.AddMovieGenreDto addMovieGenreDto)
        {
            var result = await _movieGenreService.add_MovieGenre(addMovieGenreDto);
            return Ok(result);
        }

        public async Task<IActionResult> Update([FromBody]CoreLib.Dtos.MovieGenre.UpdateMovieGenreDto updateMovieGenreDto)
        {
            var result =await _movieGenreService.uppdate_MovieGenre(updateMovieGenreDto);
            return Ok(result);
        }
        public async Task<IActionResult> Delete([FromBody] CoreLib.Dtos.MovieGenre.DeleteMovieGenreDto deleteMovieGenreDto)
        {
            var result = await _movieGenreService.delete_Episode(deleteMovieGenreDto);
            return Ok(result);

        }
    }
}
