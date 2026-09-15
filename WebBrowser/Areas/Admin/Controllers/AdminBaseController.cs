using Microsoft.AspNetCore.Mvc;
using WebBrowser.Areas.Admin.Filters;

namespace WebBrowser.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public abstract class AdminBaseController : Controller
    {
    }
}
