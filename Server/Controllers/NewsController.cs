using CoreLib.Dtos.CMS;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewsController : ControllerBase
    {
        private readonly ICNews _newsDataService;

        public NewsController(ICNews newsDataService)
        {
            _newsDataService = newsDataService;
        }

        [HttpPost("catalog")]
        public async Task<IActionResult> GetCatalog([FromBody] NewsCatalogFilterDto filter)
        {
            filter ??= new NewsCatalogFilterDto();
            var result = await _newsDataService.GetCatalogNews(filter);
            return Ok(result);
        }

        [HttpGet("detail/{id}")]
        public async Task<IActionResult> GetDetail(long id)
        {
            if (id <= 0) return BadRequest(new CResponseMessage { Success = false, code = "400", message = "ID bài viết không hợp lệ." });
            var result = await _newsDataService.GetNewsDetail(id);
            return Ok(result);
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var result = await _newsDataService.GetCategories();
            return Ok(result);
        }

        [HttpPost("seed")]
        public async Task<IActionResult> Seed()
        {
            var result = await _newsDataService.SeedSampleNews();
            return Ok(result);
        }
    }
}
