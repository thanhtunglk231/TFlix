using Microsoft.AspNetCore.Mvc;

namespace WebBrowser.Controllers
{
    public class PreviewController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
