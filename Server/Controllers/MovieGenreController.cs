using CoreLib.Dtos.Genres;
using CoreLib.Dtos.MovieGenre;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MovieGenreController : ControllerBase
    {
        private readonly ICMovieGenre _gernes;
        public MovieGenreController(ICMovieGenre gernes)
        {
            _gernes = gernes;
        }



        [HttpGet("getbyid")]
        public async Task<IActionResult> getall([FromQuery]decimal id)
        {
            var result =await _gernes.sp_get_by_movie(id);
            return Ok(result);
        }

        [HttpPost("add")]
        public async Task<IActionResult> add([FromBody] AddMovieGenreDto addGenreDto)
        {
            var res = await _gernes.sp_add(addGenreDto);
            return Ok(res);
        }

        [HttpPost("update")]
        public async Task<IActionResult> upadte([FromBody] UpdateMovieGenreDto updateGenreDto)
        {
            var res = await _gernes.sp_update(updateGenreDto);
            return Ok(res);
        }


        [HttpPost("delete")]
        public async Task<IActionResult> delete([FromBody] DeleteMovieGenreDto updateGenreDto)
        {
            var res = await _gernes.sp_delete(updateGenreDto);
            return Ok(res);
        }
    }
}
