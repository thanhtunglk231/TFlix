using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;

namespace WebBrowser.Areas.Admin.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AdminAuthorizeAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var session = context.HttpContext.Session;
            var adminToken = session.GetString("AdminJWToken");

            if (string.IsNullOrWhiteSpace(adminToken))
            {
                var request = context.HttpContext.Request;
                bool isAjax = request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                             request.Headers["Accept"].ToString().Contains("application/json") ||
                             request.Path.Value?.EndsWith(".json") == true;

                if (isAjax)
                {
                    context.Result = new JsonResult(new
                    {
                        success = false,
                        code = "401",
                        message = "Phiên đăng nhập Quản trị đã hết hạn hoặc không hợp lệ. Vui lòng đăng nhập lại."
                    })
                    { StatusCode = 401 };
                }
                else
                {
                    string returnUrl = request.Path + request.QueryString;
                    context.Result = new RedirectToActionResult("Index", "Auth", new { area = "Admin", returnUrl });
                }
                return;
            }

            await next();
        }
    }
}
