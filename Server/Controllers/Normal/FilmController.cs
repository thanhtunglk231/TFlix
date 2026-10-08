using CoreLib.Dtos;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using CoreLib.Models;
using Server.Services;

namespace Server.Controllers.Normal
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilmController : ControllerBase
    {
        private readonly ICFilm _cFilm;
        private readonly IConfiguration _configuration;
        private readonly IRedisCacheService _cache;

        public FilmController(ICFilm cFilm, IConfiguration configuration, IRedisCacheService cache)
        {
            _cFilm = cFilm;
            _configuration = configuration;
            _cache = cache;
        }

        [HttpGet("test-db")]
        public async Task<IActionResult> TestDb()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("SqlServer");
                await using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                return Ok(new { code = "200", message = "SQL Server connected successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { code = "500", message = "SQL Server connection failed: " + ex.Message });
            }
        }

        [HttpPost("GetFilmDetail")]
        public async Task<IActionResult> GetFilmDetail([FromBody] GetFilmDetail filmId)
        {
            var cacheKey = $"tflix:movies:detail:{filmId.id}:{(filmId.genre ?? string.Empty).Trim().ToLowerInvariant()}";
            var result = await _cache.GetAsync<CResponseMessage>(cacheKey)
                ?? _cFilm.Get_Film_Detail(filmId);
            if (result.Success)
                await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return Ok(result);
        }
    }
}
