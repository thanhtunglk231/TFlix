using CoreLib.Dtos.Episode;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class EsopideController : Controller
    {
        private readonly IEpisode _episodeService;

        public EsopideController(IEpisode episodeService)
        {
            _episodeService = episodeService;
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> GetAll()
        {
            Console.WriteLine("[Admin/Esopide] -> GetAll() ENTER");
            var result = await _episodeService.get_all();
            Console.WriteLine("[Admin/Esopide] <- GetAll() EXIT: " + JsonConvert.SerializeObject(result));
            return Ok(result);
        }
        public async Task<IActionResult> AddEpisode([FromBody] AddEpisodeDto dto)
        {
            Console.WriteLine("[Admin/Esopide] <- AddEpisode() Calling: ");
            var result = await _episodeService.add_Episode(dto);
            Console.WriteLine("[Admin/Esopide] <- AddEpisode() EXIT: " + JsonConvert.SerializeObject(result));
            return Ok(result);
        }

        public async Task<IActionResult> UpdateEpisode([FromBody] UpdateEpisodeDto dto)
        {
            Console.WriteLine("[Admin/Esopide] <- UpdateEpisode() Calling: ");
            var result = await _episodeService.uppdate_Episode(dto);
            return Ok(result);
        }

        public async Task<IActionResult> Delete([FromQuery] decimal id)
        {
            Console.WriteLine("[Admin/Esopide] -> Delete() ENTER, id=" + id);
            if (id <= 0) return BadRequest(new { message = "Invalid id." });
            var result = await _episodeService.delete_Episode(id);
            Console.WriteLine("[Admin/Esopide] <- Delete() EXIT: " + JsonConvert.SerializeObject(result));
            return Ok(result);
        }
    }
}
