using Microsoft.AspNetCore.Mvc;

namespace WebBrowser.Areas.Admin.Controllers
{
    public class HomeController : AdminBaseController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
