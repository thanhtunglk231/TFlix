using CoreLib.Dtos.MovieGenre;
using CoreLib.Dtos.SeriesGenre;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeriesGenresController : ControllerBase
    {

        private readonly ICSeriesGenres _gernes;
        public SeriesGenresController(ICSeriesGenres gernes)
        {
            _gernes = gernes;
        }



        [HttpGet("getbyid")]
        public async Task<IActionResult> getall([FromQuery] decimal id)
        {
            var result = _gernes.sp_get_by_series(id);
            return Ok(result);
        }

        [HttpPost("add")]
        public async Task<IActionResult> add([FromBody] AddSeriesGenreDto addGenreDto)
        {
            var res = _gernes.sp_add(addGenreDto);
            return Ok(res);
        }

        [HttpPost("update")]
        public async Task<IActionResult> upadte([FromBody] UpdateSeriesGenreDto updateGenreDto)
        {
            var res = _gernes.sp_update(updateGenreDto);
            return Ok(res);
        }


        [HttpPost("delete")]
        public async Task<IActionResult> delete([FromBody] DeleteSeriesGenreDto updateGenreDto)
        {
            var res = _gernes.sp_delete(updateGenreDto);
            return Ok(res);
        }
    }
}
