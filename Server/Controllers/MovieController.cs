using CoreLib.Dtos.Movies;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Server.Services;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MovieController : ControllerBase
    {

        private readonly ICMovie _cMovie;
        private readonly IRedisCacheService _cache;
        private static readonly TimeSpan MovieCacheDuration = TimeSpan.FromMinutes(10);

        public MovieController(ICMovie cMovie, IRedisCacheService cache)
        {
            _cMovie = cMovie;
            _cache = cache;
        }
        [HttpGet("getall")]
        public async Task<IActionResult> GetAllMovies([FromQuery] long? userId = null)
        {
            var cacheKey = userId.HasValue ? $"tflix:movies:all:u_{userId.Value}" : "tflix:movies:all";
            var response = await _cache.GetAsync<CoreLib.Models.CResponseMessage>(cacheKey)
                ?? await _cMovie.get_all(userId);
            if (response == null)
                return StatusCode(500, new { code = "500", message = "Null response from service" });

            if (response.Success)
                await _cache.SetAsync(cacheKey, response, MovieCacheDuration);

            return Ok(new { code = response.code, success = response.Success, message = response.message, Data = response.Data });
        }

        [HttpGet("autocomplete")]
        public async Task<IActionResult> Autocomplete([FromQuery(Name = "q")] string query, [FromQuery] int limit = 8)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Ok(new
                {
                    code = "200",
                    success = true,
                    message = "",
                    Data = Array.Empty<MovieAutocompleteItemDto>()
                });
            }

            var request = new MovieAutocompleteQueryDto
            {
                Query = query,
                Limit = Math.Clamp(limit, 1, 12)
            };
            var cacheKey = $"tflix:movies:autocomplete:{HashKey($"{request.Query.Trim().ToLowerInvariant()}|{request.Limit}")}";
            var response = await _cache.GetAsync<CoreLib.Models.CResponseMessage>(cacheKey)
                ?? await _cMovie.Autocomplete(request);
            if (response.Success)
                await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(2));
            return Ok(new
            {
                code = response.code,
                success = response.Success,
                message = response.message,
                Data = response.Data
            });
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

            if (response.Success)
                await _cache.RemoveByPrefixAsync("tflix:movies:");

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
            if (response.Success)
                await _cache.RemoveByPrefixAsync("tflix:movies:");
            return Ok(response);
        }

        [HttpPost("delete")]
        public async Task<IActionResult> deletemovie([FromBody] IdRequest req)
        {
            if (req == null || req.id <= 0) return BadRequest(new { code = "400", message = "Invalid id." });
            var response = await _cMovie.Delete_movie(req.id, req.userId);
            if (response == null) return StatusCode(500, new { code = "500", message = "Null response from service" });
            if (response.Success)
                await _cache.RemoveByPrefixAsync("tflix:movies:");
            return Ok(response);
        }

        [HttpGet("catalog")]
        public async Task<IActionResult> GetCatalogMovies([FromQuery] MovieCatalogFilterDto filter)
        {
            filter ??= new MovieCatalogFilterDto();
            var cacheKey = GetCatalogCacheKey(filter);
            var response = await _cache.GetAsync<CoreLib.Models.CResponseMessage>(cacheKey)
                ?? await _cMovie.GetCatalogMovies(filter);
            if (response == null) return StatusCode(500, new { code = "500", message = "Null response from service" });

            if (response.Success)
                await _cache.SetAsync(cacheKey, response, MovieCacheDuration);

            return Ok(response);
        }

        [HttpPost("catalog")]
        public async Task<IActionResult> PostCatalogMovies([FromBody] MovieCatalogFilterDto filter)
        {
            filter ??= new MovieCatalogFilterDto();
            var cacheKey = GetCatalogCacheKey(filter);
            var response = await _cache.GetAsync<CoreLib.Models.CResponseMessage>(cacheKey)
                ?? await _cMovie.GetCatalogMovies(filter);
            if (response == null) return StatusCode(500, new { code = "500", message = "Null response from service" });

            if (response.Success)
                await _cache.SetAsync(cacheKey, response, MovieCacheDuration);

            return Ok(response);
        }

        [HttpPost("seed")]
        public async Task<IActionResult> SeedSampleMovies()
        {
            var response = await _cMovie.SeedSampleMovies();
            if (response == null) return StatusCode(500, new { code = "500", message = "Null response from service" });
            if (response.Success)
                await _cache.RemoveByPrefixAsync("tflix:movies:");
            return Ok(response);
        }

        private static string GetCatalogCacheKey(MovieCatalogFilterDto filter)
            => $"tflix:movies:catalog:{HashKey(JsonConvert.SerializeObject(filter))}";

        private static string HashKey(string value)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

        public class IdRequest { public decimal id { get; set; } public long? userId { get; set; } }
    }
}
