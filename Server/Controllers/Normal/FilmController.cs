using CoreLib.Dtos;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
                using (var conn = new SqlConnection("ur_key"))
                {
                    await conn.OpenAsync();

        [HttpPost("GetFilmDetail")]
        public async Task<IActionResult> GetFilmDetail([FromBody] GetFilmDetail filmId)
        {
            var result =  _cFilm.Get_Film_Detail(filmId);
            return Ok(result);
        }
    }
}
