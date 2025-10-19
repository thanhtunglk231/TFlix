using Microsoft.AspNetCore.Mvc;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHomeService _homeService;
        public HomeController(IHomeService homeService) => _homeService = homeService;

        public IActionResult Index() => View();

        // Ajax endpoint cho jQuery � KH�NG nh?n limit
        [HttpGet]
        public async Task<IActionResult> MoviesLatest()
        {
            var result = await _homeService.get_Movie_Lastest_Item(); // ?? kh�ng tham s?
            return Json(result);
        }

        public IActionResult Preview() => View();
        public IActionResult Privacy() => View();
    }
}
