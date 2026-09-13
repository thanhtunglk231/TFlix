using Microsoft.AspNetCore.Mvc;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHomeService _homeService;
        private readonly IMovieService _movieService;

        public HomeController(IHomeService homeService, IMovieService movieService)
        {
            _homeService = homeService;
            _movieService = movieService;
        }

        public IActionResult Index() => View();

        // Ajax endpoint cho jQuery � KH�NG nh?n limit
        [HttpGet]
        public async Task<IActionResult> MoviesLatest()
        {
            var result = await _homeService.get_Movie_Lastest_Item(); // ?? kh�ng tham s?
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> MovieAutocomplete(string q, int limit = 8)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Json(new { code = "200", success = true, message = "", data = Array.Empty<object>() });
            }

            var result = await _movieService.Autocomplete(q, limit);
            return Json(result);
        }


        //[HttpGet]
        //public async Task<IActionResult>get

        public IActionResult Preview() => View();
        public IActionResult Privacy() => View();
    }
}
