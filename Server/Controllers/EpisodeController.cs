// Server/Controllers/EpisodeController.cs
using CoreLib.Dtos.Episode;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EpisodeController : ControllerBase
    {
        private readonly ICEpisode _cEpisode;
        public EpisodeController(ICEpisode cEpisode) => _cEpisode = cEpisode;

        [HttpGet("getall")]
        public async Task<IActionResult> GetAll()
        {
            var response = await _cEpisode.sp_get_all_episode();
            if (response.code != "200")
                return StatusCode(500, new { code = response.code, message = response.message });

            return Ok(new { code = response.code, message = response.message, data = response.Data });
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] AddEpisodeDto addEpisodeDto)
        {
            if (addEpisodeDto == null || string.IsNullOrWhiteSpace(addEpisodeDto.Title))
                return BadRequest(new { code = "400", message = "Invalid episode data." });

            var response = await _cEpisode.Add_episode(addEpisodeDto);
            if (response.code != "200")
                return StatusCode(500, new { code = response.code, message = response.message });

            return Ok(new { code = response.code, message = response.message, data = response.Data });
        }

        [HttpPost("update")]
        public async Task<IActionResult> Update([FromBody] UpdateEpisodeDto dto)
        {
            if (dto == null || dto.EpisodeId <= 0 || string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(new { code = "400", message = "Invalid episode data." });

            var response = await _cEpisode.Update_episode(dto);
            if (response.code != "200")
                return StatusCode(500, new { code = response.code, message = response.message });

            return Ok(new { code = response.code, message = response.message, data = response.Data });
        }

        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromBody] decimal id)
        {
            if (id <= 0) return BadRequest(new { code = "400", message = "Invalid id." });

            var response = await _cEpisode.Delete_episode(id);
            if (response.code != "200")
                return StatusCode(500, new { code = response.code, message = response.message });

            return Ok(new { code = response.code, message = response.message });
        }
    }
}
