using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Areas.Admin.Filters;
using WebBrowser.Models.AuthModels;

namespace WebBrowser.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public abstract class AdminBaseController : Controller
    {
        protected long? GetCurrentAdminUserId()
        {
            var json = HttpContext.Session.GetString("AdminCurrentUser");
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var user = JsonConvert.DeserializeObject<UserInfo>(json);
                return user?.userId > 0 ? (long)user.userId : null;
            }
            catch
            {
                return null;
            }
        }

        protected bool IsSuperAdmin()
        {
            var user = GetCurrentAdminUser();
            return user?.roles.Any(role => string.Equals(role, "SUPER_ADMIN", StringComparison.OrdinalIgnoreCase)) == true;
        }

        protected bool HasAdminRole()
        {
            var user = GetCurrentAdminUser();
            return user?.roles.Any(role => string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "SUPER_ADMIN", StringComparison.OrdinalIgnoreCase)) == true;
        }

        protected long? GetOwnershipFilterUserId() => IsSuperAdmin() ? null : GetCurrentAdminUserId();

        private UserInfo? GetCurrentAdminUser()
        {
            var json = HttpContext.Session.GetString("AdminCurrentUser");
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return JsonConvert.DeserializeObject<UserInfo>(json); }
            catch { return null; }
        }
    }
}
