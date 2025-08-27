using CoreLib.Dtos.Season;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SeasonController : Controller
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
    }
}
