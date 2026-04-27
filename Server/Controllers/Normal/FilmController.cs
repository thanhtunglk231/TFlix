using CoreLib.Dtos;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace Server.Controllers.Normal
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilmController : ControllerBase
    {
        private readonly ICFilm _cFilm ;
        public FilmController( ICFilm cFilm)
        {
            _cFilm = cFilm;
        }
        [HttpGet("test-db")]
        public async Task<IActionResult> TestDb()
        {
            try
            {
                using (var conn = new SqlConnection("Server=tcp:rg-sql-demo-toto.database.windows.net,1433;Initial Catalog=free-sql-db-9132213;User ID=toto;Password=123456789@Aa;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"))
                {
                    await conn.OpenAsync();

                    return Ok(new
                    {
                        status = "OK",
                        message = "Kết nối DB thành công"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "ERROR",
                    message = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }
        [HttpPost("GetFilmDetail")]
        public async Task<IActionResult> GetFilmDetail([FromBody] GetFilmDetail filmId)
        {
            var result =  _cFilm.Get_Film_Detail(filmId);
            return Ok(result);
        }
        [HttpGet("ip")]
        public IActionResult GetIP()
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            return Ok(ip);
        }
    }
}
