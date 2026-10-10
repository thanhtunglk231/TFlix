using CoreLib.Dtos.Genres;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GenresController : ControllerBase
    {
        private readonly ICGenres _gernes;  
        public GenresController(ICGenres gernes)
        {
            _gernes = gernes;
        }



        [HttpGet("getall")]
        public async Task<IActionResult> getall()
        {
            var result = await _gernes.GetAll();
            return Ok(result);
        }

        [HttpPost("add")]
        public async Task<IActionResult> add([FromBody]AddGenreDto addGenreDto)
        {
            if (addGenreDto == null || string.IsNullOrWhiteSpace(addGenreDto.genreName))
                return BadRequest(new { code = "400", success = false, message = "Tên thể loại là bắt buộc." });

            var res = await _gernes.Add(addGenreDto);
            return Ok(res);
        }

        [HttpPost("update")]
        public async Task<IActionResult> upadte([FromBody]UpdateGenreDto updateGenreDto)
        {
            if (updateGenreDto == null || updateGenreDto.GenreId <= 0 || string.IsNullOrWhiteSpace(updateGenreDto.genreName))
                return BadRequest(new { code = "400", success = false, message = "Dữ liệu thể loại không hợp lệ." });

            var res = await _gernes.Update(updateGenreDto);
            return Ok(res);
        }


        [HttpPost("delete")]
        public async Task<IActionResult> delete([FromBody] decimal updateGenreDto)
        {
            if (updateGenreDto <= 0)
                return BadRequest(new { code = "400", success = false, message = "GenreId không hợp lệ." });

            var res = await _gernes.Delete(updateGenreDto);
            return Ok(res);
        }
    }


}
