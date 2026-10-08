using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using CoreLib.Models;
using Server.Services;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly ICHome _homeService;
        private readonly IRedisCacheService _cache;

        public HomeController(ICHome homeService, IRedisCacheService cache)
        {
            _homeService = homeService;
            _cache = cache;
        }

        [HttpGet("MovieLastestItem")]
        public async Task<IActionResult> MovieLastestItem()
        {
            const string cacheKey = "tflix:movies:home:latest";
            var result = await _cache.GetAsync<CResponseMessage>(cacheKey)
                ?? await _homeService.MovieLastestItem();

            if (result.Success)
                await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));

            return Ok(result);
        }
    }
}
