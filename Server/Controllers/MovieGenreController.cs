using CoreLib.Dtos.Genres;
using CoreLib.Dtos.MovieGenre;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CoreLib.Models;
using Server.Services;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MovieGenreController : ControllerBase
    {
        private readonly ICMovieGenre _gernes;
        private readonly IRedisCacheService _cache;
        public MovieGenreController(ICMovieGenre gernes, IRedisCacheService cache)
        {
            _gernes = gernes;
            _cache = cache;
        }



        [HttpGet("getbyid")]
        public async Task<IActionResult> getall([FromQuery]decimal id)
        {
            var cacheKey = $"tflix:movies:genres:{id}";
            var result = await _cache.GetAsync<CResponseMessage>(cacheKey) ?? await _gernes.sp_get_by_movie(id);
            if (result.Success)
                await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return Ok(result);
        }

        [HttpPost("add")]
        public async Task<IActionResult> add([FromBody] AddMovieGenreDto addGenreDto)
        {
            var res = await _gernes.sp_add(addGenreDto);
            if (res.Success)
                await _cache.RemoveByPrefixAsync("tflix:movies:");
            return Ok(res);
        }

        [HttpPost("update")]
        public async Task<IActionResult> upadte([FromBody] UpdateMovieGenreDto updateGenreDto)
        {
            var res = await _gernes.sp_update(updateGenreDto);
            if (res.Success)
                await _cache.RemoveByPrefixAsync("tflix:movies:");
            return Ok(res);
        }


        [HttpPost("delete")]
        public async Task<IActionResult> delete([FromBody] DeleteMovieGenreDto updateGenreDto)
        {
            var res = await _gernes.sp_delete(updateGenreDto);
            if (res.Success)
                await _cache.RemoveByPrefixAsync("tflix:movies:");
            return Ok(res);
        }
    }
}
