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

        [HttpGet("series/{seriesId:long}")]
        public async Task<IActionResult> GetBySeries(long seriesId, [FromQuery] bool publishedOnly = true)
        {
            if (seriesId <= 0)
                return BadRequest(new { code = "400", message = "SeriesId không hợp lệ." });

            var response = await _cEpisode.GetBySeriesAsync(seriesId, publishedOnly);
            if (response.code != "200")
            {
                var statusCode = response.code switch
                {
                    "400" => StatusCodes.Status400BadRequest,
                    "503" => StatusCodes.Status503ServiceUnavailable,
                    _ => StatusCodes.Status500InternalServerError
                };
                return StatusCode(statusCode, new { code = response.code, message = response.message });
            }

            return Ok(new { code = response.code, message = response.message, data = response.Data });
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] AddEpisodeDto addEpisodeDto)
        {
            if (addEpisodeDto == null || addEpisodeDto.SeriesId <= 0 || addEpisodeDto.SeasonId <= 0 ||
                addEpisodeDto.EpisodeNo <= 0 || string.IsNullOrWhiteSpace(addEpisodeDto.Title))
                return BadRequest(new { code = "400", message = "Dữ liệu tập phim không hợp lệ." });

            var response = await _cEpisode.Add_episode(addEpisodeDto);
            if (response.code != "200")
                return StatusCode(response.code == "400" ? 400 : 500, new { code = response.code, message = response.message });

            return Ok(new { code = response.code, message = response.message, data = response.Data });
        }

        [HttpPost("update")]
        public async Task<IActionResult> Update([FromBody] UpdateEpisodeDto dto)
        {
            if (dto == null || dto.EpisodeId <= 0 || dto.SeriesId <= 0 || dto.SeasonId <= 0 ||
                dto.EpisodeNo <= 0 || string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(new { code = "400", message = "Dữ liệu tập phim không hợp lệ." });

            var response = await _cEpisode.Update_episode(dto);
            if (response.code != "200")
                return StatusCode(response.code == "400" ? 400 : 500, new { code = response.code, message = response.message });

            return Ok(new { code = response.code, message = response.message, data = response.Data });
        }

        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromBody] long id)
        {
            if (id <= 0) return BadRequest(new { code = "400", message = "Invalid id." });

            var response = await _cEpisode.Delete_episode(id);
            if (response.code != "200")
                return StatusCode(response.code == "400" ? 400 : 500, new { code = response.code, message = response.message });

            return Ok(new { code = response.code, message = response.message });
        }
    }
}
