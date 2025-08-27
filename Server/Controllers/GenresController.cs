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
            var result= _gernes.GetAll();
            return Ok(result);
        }

        [HttpPost("add")]
        public async Task<IActionResult> add([FromBody]AddGenreDto addGenreDto)
        {
            var res = _gernes.Add(addGenreDto);
            return Ok(res);
        }

        [HttpPost("update")]
        public async Task<IActionResult> upadte([FromBody]UpdateGenreDto updateGenreDto)
        {
            var res= _gernes.Update(updateGenreDto);
            return Ok(res);
        }


        [HttpPost("delete")]
        public async Task<IActionResult> delete([FromBody] decimal updateGenreDto)
        {
            var res = _gernes.Delete(updateGenreDto);
            return Ok(res);
        }
    }


}
