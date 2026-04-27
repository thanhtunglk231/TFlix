using Microsoft.AspNetCore.Mvc;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHomeService _homeService;
        public HomeController(IHomeService homeService) => _homeService = homeService;

        public IActionResult Index() => View();

        // Ajax endpoint cho jQuery – KHÔNG nh?n limit
        [HttpGet]
        public async Task<IActionResult> MoviesLatest()
        {
            var result = await _homeService.get_Movie_Lastest_Item(); // ?? không tham s?
            return Json(result);
        }
        [HttpGet]
        public async Task<IActionResult> getEpisodeLatest()
        {
            var result = await _homeService.get_episode_latest(); // ?? không tham s?
            return Json(result);
        }
        [HttpGet]
        public async Task<IActionResult> SearchContents([FromQuery] string keyword)
        {
            var result = await _homeService.search_contents(keyword);
            return Json(result);
        }
        //[HttpGet]
        //public async Task<IActionResult>get

        public IActionResult Preview() => View();
        public IActionResult Privacy() => View();
    }
}
