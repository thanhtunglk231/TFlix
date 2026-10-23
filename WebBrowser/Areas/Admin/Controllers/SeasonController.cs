using CoreLib.Dtos.Episode;
using CoreLib.Dtos.Season;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Areas.Admin.Controllers
{
    public class SeasonController : AdminBaseController
    {
        private readonly ISesonService _seasonService;
        public SeasonController(ISesonService seasonService)
        {
            _seasonService = seasonService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> GetAll()
        {
            Console.WriteLine("[Admin/SeasonController] -> GetAll() ENTER");
            var result = await _seasonService.get_all();
            Console.WriteLine("[Admin/SeasonController] <- GetAll() EXIT: " + JsonConvert.SerializeObject(result));
            return Ok(result);
        }

        public async Task<IActionResult> AddSeason([FromBody] AddSeasonDto dto)
        {
            Console.WriteLine("[Admin/SeasonController] <- AddSeason() Calling: ");
            var result = await _seasonService.add_season(dto);
            Console.WriteLine("[Admin/SeasonController] <- AddSeason() EXIT: " + JsonConvert.SerializeObject(result));
            return Ok(result);
        }

        public async Task<IActionResult> UpdateSeason([FromBody] UpdateSeasonDto dto)
        {
            Console.WriteLine("[Admin/SeasonController] <- UpdateSeason() Calling: ");
            var result = await _seasonService.uppdate_Season(dto);
            return Ok(result);
        }

        public async Task<IActionResult> Delete([FromQuery] decimal id)
        {
            Console.WriteLine("[Admin/SeasonController] -> Delete() ENTER, id=" + id);
            if (id <= 0) return BadRequest(new { message = "Invalid id." });
            var result = await _seasonService.delete_Season(id);
            Console.WriteLine("[Admin/SeasonController] <- Delete() EXIT: " + JsonConvert.SerializeObject(result));
            return Ok(result);
        }

        // ===== Quan ly Movie trong Season =====

        [HttpPost]
        public async Task<IActionResult> AssignMovie([FromBody] AssignMovieToSeasonDto dto)
        {
            Console.WriteLine("[Admin/SeasonController] -> AssignMovie() ENTER");
            if (dto == null || dto.SeriesId <= 0 || dto.SeasonId <= 0 || dto.MovieId <= 0 || dto.EpisodeNo <= 0)
                return BadRequest(new { message = "Du lieu khong hop le." });
            var result = await _seasonService.AssignMovie(dto);
            Console.WriteLine("[Admin/SeasonController] <- AssignMovie() EXIT: " + JsonConvert.SerializeObject(result));
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateMovie([FromBody] UpdateMovieInSeasonDto dto)
        {
            Console.WriteLine("[Admin/SeasonController] -> UpdateMovie() ENTER");
            if (dto == null || dto.EpisodeId <= 0 || dto.EpisodeNo <= 0)
                return BadRequest(new { message = "Du lieu khong hop le." });
            var result = await _seasonService.UpdateMovieInSeason(dto);
            Console.WriteLine("[Admin/SeasonController] <- UpdateMovie() EXIT: " + JsonConvert.SerializeObject(result));
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveMovie([FromBody] long episodeId)
        {
            Console.WriteLine("[Admin/SeasonController] -> RemoveMovie() ENTER episodeId=" + episodeId);
            if (episodeId <= 0) return BadRequest(new { message = "EpisodeId khong hop le." });
            var result = await _seasonService.RemoveMovie(episodeId);
            Console.WriteLine("[Admin/SeasonController] <- RemoveMovie() EXIT: " + JsonConvert.SerializeObject(result));
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetMoviesBySeason([FromQuery] long seasonId)
        {
            Console.WriteLine("[Admin/SeasonController] -> GetMoviesBySeason() ENTER seasonId=" + seasonId);
            if (seasonId <= 0) return BadRequest(new { message = "SeasonId khong hop le." });
            var result = await _seasonService.GetMoviesBySeason(seasonId);
            Console.WriteLine("[Admin/SeasonController] <- GetMoviesBySeason() EXIT: " + JsonConvert.SerializeObject(result));
            return Ok(result);
        }
    }
}
