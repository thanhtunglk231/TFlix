using Microsoft.AspNetCore.Mvc;

namespace WebBrowser.Controllers
{
    public class FilmController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

    }
}
