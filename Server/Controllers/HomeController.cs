using DataServiceLib.Implements;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly ICHome _homeService;
        public HomeController(ICHome homeService)
        {
            _homeService = homeService;
        }
        [HttpGet("MovieLastestItem")]
        public async Task<IActionResult> MovieLastestItem()
        {
            var result = await _homeService.MovieLastestItem();
            return Ok(result);
        }

        [HttpGet("search")]
        public IActionResult Search([FromQuery] string keyword)
        {
            var result = _homeService.Search(keyword);
            return Ok(result);
        }

        [HttpGet("EpisodeLatestItem")]
        public async Task<IActionResult> EpisodeLatestItem()
        {
            var result = await _homeService.EpisodeLatestItem();
            return Ok(result);
        }
    }
}
