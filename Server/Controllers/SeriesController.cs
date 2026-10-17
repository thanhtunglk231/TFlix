using CoreLib.Dtos.Movies;
using CoreLib.Dtos.Series;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeriesController : ControllerBase
    {

        private readonly ICSeries _cMovie;
        public SeriesController(ICSeries cMovie)
        {
            _cMovie = cMovie;
        }
        [HttpGet("getall")]
        public async Task<IActionResult> GetAllMovies([FromQuery] long? userId = null)
        {
            var response = await _cMovie.get_all(userId);
            if (response.code != "200")
            {
                return StatusCode(500, new { code = response.code, message = response.message });
            }
            return Ok(new { code = response.code, message = response.message, data = response.Data });
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddMovie([FromBody] AddSeriesDto addMovieDto)
        {
            Console.WriteLine("[API/Series] -> Add ENTER");
            Console.WriteLine("[API/Series] Body DTO: " + JsonConvert.SerializeObject(addMovieDto));
            Console.WriteLine("[API/Series] ModelState.IsValid: " + ModelState.IsValid);
            if (addMovieDto == null || string.IsNullOrWhiteSpace(addMovieDto.Title))
            {
                return BadRequest(new { code = "400", message = "Invalid movie data." });
            }
            var response = await _cMovie.Add_series(addMovieDto);
            if (response.code != "200")
            {
                return StatusCode(500, new { code = response.code, message = response.message });
            }
            return Ok(new { code = response.code, message = response.message, data = response.Data });
        }

        [HttpPost("update")]
        public async Task<IActionResult> updatemovie([FromBody] UpdateSeriesDto addMovieDto)
        {
            if (addMovieDto == null || string.IsNullOrWhiteSpace(addMovieDto.Title))
            {
                return BadRequest(new { code = "400", message = "Invalid movie data." });
            }
            var response = await _cMovie.Update_series(addMovieDto);
            if (response.code != "200")
            {
                return StatusCode(500, new { code = response.code, message = response.message });
            }
            return Ok(new { code = response.code, message = response.message, data = response.Data });
        }

        [HttpPost("delete")]
        public async Task<IActionResult> deletemovie([FromBody] DeleteSeriesRequest request)
        {
            if (request.Id <= 0) return BadRequest(new { code = "400", message = "Invalid series id." });
            var response = await _cMovie.Delete_series(request.Id, request.UserId);
            if (response.code != "200")
            {
                return StatusCode(500, new { code = response.code, message = response.message });
            }
            return Ok(response);
        }

        public sealed class DeleteSeriesRequest
        {
            public decimal Id { get; set; }
            public long? UserId { get; set; }
        }
    }
}
