using CoreLib.Dtos.Movies;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class MovieController : Controller
    {
        private readonly IMovieService _movieService;
        public MovieController(IMovieService movieService)
        {
            _movieService = movieService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> GetAll()
        {
            Console.WriteLine("[Admin/MovieController] -> GetAll() ENTER");
            var result = await _movieService.get_all();
            return Ok(result);
        }
        public async Task<IActionResult> AddMovie([FromBody] AddMovieDto dto)
        {
            Console.WriteLine("[Admin/MovieController] <- AddMovie() Calling: ");
            var result = await _movieService.add_Movie(dto);
            Console.WriteLine("[Admin/MovieController] <- AddMovie() EXIT: " + JsonConvert.SerializeObject(result));
            return Ok(result);
        }
        public async Task<IActionResult> UpdateMovie([FromBody] AddMovieDto dto)
        {
            Console.WriteLine("[Admin/MovieController] <- UpdateMovie() Calling: ");
            var result = await _movieService.uppdate_Movie(dto);
            Console.WriteLine("[Admin/MovieController] <- UpdateMovie() EXIT: " + JsonConvert.SerializeObject(result));
            return Ok(result);
        }
        public async Task<IActionResult> Delete([FromQuery] decimal id)
        {
            Console.WriteLine("[Admin/MovieController] -> Delete() ENTER, id=" + id);
            if (id <= 0) return BadRequest(new { message = "Invalid id." });
            var result = await _movieService.delete_Season(id);
            Console.WriteLine("[Admin/MovieController] <- Delete() EXIT: " + JsonConvert.SerializeObject(result));
            return Ok(result);
        }
    }
}
