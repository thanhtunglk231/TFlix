using CoreLib.Dtos;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Server.Controllers.Normal
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilmController : ControllerBase
    {
        private readonly ICFilm _cFilm;
        private readonly IConfiguration _configuration;

        public FilmController(ICFilm cFilm, IConfiguration configuration)
        {
            _cFilm = cFilm;
            _configuration = configuration;
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
            var result = _cFilm.Get_Film_Detail(filmId);
            return Ok(result);
        }
    }
}
