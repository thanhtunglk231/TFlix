using CoreLib.Dtos.Movies;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MovieController : ControllerBase
    {

        private readonly ICMovie _cMovie;
        public MovieController(ICMovie cMovie)
        {
            _cMovie = cMovie;
        }
        [HttpGet("getall")]
        public async Task<IActionResult> GetAllMovies()
        {
            var response = await _cMovie.get_all();
            if (response == null)
                return StatusCode(500, new { code = "500", message = "Null response from service" });

            return Ok(new { code = response.code, success = response.Success, message = response.message, Data = response.Data });
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddMovie([FromBody] AddMovieDto addMovieDto)
        {
            if (addMovieDto == null || string.IsNullOrWhiteSpace(addMovieDto.Title))
            {
                return BadRequest(new { code = "400", message = "Invalid movie data." });
            }
            var response = await _cMovie.Add_movie(addMovieDto);
            if (response == null) return StatusCode(500, new { code = "500", message = "Null response from service" });

            // Include returned data (e.g., new MovieId) in the CResponseMessage.Data if needed
            return Ok(response);
        }

        [HttpPost("update")]
        public async Task<IActionResult> updatemovie([FromBody] UpdateMovieDto addMovieDto)
        {
            if (addMovieDto == null || string.IsNullOrWhiteSpace(addMovieDto.Title))
            {
                return BadRequest(new { code = "400", message = "Invalid movie data." });
            }
            var response = await _cMovie.Update_movie(addMovieDto);
            if (response == null) return StatusCode(500, new { code = "500", message = "Null response from service" });
            return Ok(response);
        }

        [HttpPost("delete")]
        public async Task<IActionResult> deletemovie([FromBody] IdRequest req)
        {
            if (req == null || req.id <= 0) return BadRequest(new { code = "400", message = "Invalid id." });
            var response = await _cMovie.Delete_movie(req.id);
            if (response == null) return StatusCode(500, new { code = "500", message = "Null response from service" });
            return Ok(response);
        }

        public class IdRequest { public decimal id { get; set; } }
    }
}
