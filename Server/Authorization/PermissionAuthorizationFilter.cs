using CoreLib.Dtos;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Server.Authorization
{
    public class PermissionAuthorizationFilter : IAsyncActionFilter
    {
        private static readonly HashSet<string> ProtectedControllers = new(StringComparer.OrdinalIgnoreCase)
        {
            "Genres",
            "Movie",
            "MovieAsset",
            "MovieGenre",
            "Series",
            "SeriesGenres",
            "Season",
            "Episode",
            "EpisodeAssert",
            "VideoSources"
        };

        private readonly ICPermission _permission;

        public PermissionAuthorizationFilter(ICPermission permission)
        {
            _permission = permission;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var controller = context.RouteData.Values["controller"]?.ToString() ?? string.Empty;
            if (!ProtectedControllers.Contains(controller))
            {
                await next();
                return;
            }

            var action = context.RouteData.Values["action"]?.ToString() ?? string.Empty;
            var permissionCode = MapPermissionCode(action, context.HttpContext.Request.Method);

            var email = context.HttpContext.User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(email))
            {
                context.Result = Forbidden("Thiếu token đăng nhập hoặc token không có email.");
                return;
            }

            var result = await _permission.CheckUserPermission(new CheckPermissionDto
            {
                Email = email,
                ScreenCode = controller,
                PermissionCode = permissionCode
            });

            if (!result.Success)
            {
                context.Result = Forbidden($"Không đủ quyền {permissionCode} trên màn {controller}.");
                return;
            }

            await next();
        }

        private static string MapPermissionCode(string action, string method)
        {
            if (action.Contains("delete", StringComparison.OrdinalIgnoreCase))
                return "Delete";

            if (action.Contains("update", StringComparison.OrdinalIgnoreCase)
                || action.Contains("replace", StringComparison.OrdinalIgnoreCase)
                || method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
                || method.Equals("PATCH", StringComparison.OrdinalIgnoreCase))
                return "Update";

            if (action.Contains("add", StringComparison.OrdinalIgnoreCase)
                || action.Contains("create", StringComparison.OrdinalIgnoreCase)
                || action.Contains("upload", StringComparison.OrdinalIgnoreCase)
                || method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                return "Create";

            return "View";
        }

        private static ObjectResult Forbidden(string message)
        {
            return new ObjectResult(new
            {
                code = "403",
                Success = false,
                message
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
