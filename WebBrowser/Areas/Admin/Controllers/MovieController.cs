using CoreLib.Dtos.Movies;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Areas.Admin.Controllers
{
    public class MovieController : AdminBaseController
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
            var currentUserId = GetCurrentAdminUserId();
            Console.WriteLine("[Admin/MovieController] -> GetAll() ENTER for userId=" + currentUserId);
            var result = await _movieService.get_all(currentUserId);
            return Ok(result);
        }
        public async Task<IActionResult> AddMovie([FromBody] AddMovieDto dto)
        {
            dto.CreatedBy = GetCurrentAdminUserId();
            Console.WriteLine("[Admin/MovieController] <- AddMovie() Calling by userId: " + dto.CreatedBy);
            var result = await _movieService.add_Movie(dto);
            Console.WriteLine("[Admin/MovieController] <- AddMovie() EXIT: " + JsonConvert.SerializeObject(result));
            return Ok(result);
        }
        public async Task<IActionResult> UpdateMovie([FromBody] UpdateMovieDto dto)
        {
            dto.UserId = GetCurrentAdminUserId();
            Console.WriteLine("[Admin/MovieController] <- UpdateMovie() Calling: "+ dto.MovieId + " by userId: " + dto.UserId);
            var result = await _movieService.uppdate_Movie(dto);
            Console.WriteLine("[Admin/MovieController] <- UpdateMovie() EXIT: " + JsonConvert.SerializeObject(result));
            return Ok(result);
        }
        public async Task<IActionResult> Delete([FromQuery] decimal id)
        {
            var currentUserId = GetCurrentAdminUserId();
            Console.WriteLine("[Admin/MovieController] -> Delete() ENTER, id=" + id + ", userId=" + currentUserId);
            if (id <= 0) return BadRequest(new { message = "Invalid id." });
            var result = await _movieService.delete_Season(id, currentUserId);
            Console.WriteLine("[Admin/MovieController] <- Delete() EXIT: " + JsonConvert.SerializeObject(result));
            return Ok(result);
        }
    }
}
