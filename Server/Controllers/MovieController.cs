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
            if (response.code != "200")
            {
                return StatusCode(500, new { code = response.code, message = response.message });
            }
            return Ok(new { code = response.code, message = response.message, data = response.Data });
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddMovie([FromBody] AddMovieDto addMovieDto)
        {
            if (addMovieDto == null || string.IsNullOrWhiteSpace(addMovieDto.Title))
            {
                return BadRequest(new { code = "400", message = "Invalid movie data." });
            }
            var response = await _cMovie.Add_movie(addMovieDto);
            if (response.code != "200")
            {
                return StatusCode(500, new { code = response.code, message = response.message });
            }
            return Ok(new { code = response.code, message = response.message, data = response.Data });
        }

        [HttpPost("update")]
        public async Task<IActionResult> updatemovie([FromBody] UpdateMovieDto addMovieDto)
        {
            if (addMovieDto == null || string.IsNullOrWhiteSpace(addMovieDto.Title))
            {
                return BadRequest(new { code = "400", message = "Invalid movie data." });
            }
            var response = await _cMovie.Update_movie(addMovieDto);
            if (response.code != "200")
            {
                return StatusCode(500, new { code = response.code, message = response.message });
            }
            return Ok(new { code = response.code, message = response.message, data = response.Data });
        }

        [HttpPost("delete")]
        public async Task<IActionResult> deletemovie([FromBody] decimal id)
        {
           
            var response = await _cMovie.Delete_movie(id);
            if (response.code != "200")
            {
                return StatusCode(500, new { code = response.code, message = response.message });
            }
            return Ok(response);
        }
    }
}
