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

    }
}
