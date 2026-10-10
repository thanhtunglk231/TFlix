using CoreLib.Dtos;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Areas.Admin.Controllers
{
    public class PermissionController : AdminBaseController
    {
        private readonly IPermissionService _permissionService;

        public PermissionController(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        public IActionResult Index()
        {
            return View("/Areas/Admin/Views/Home/Permission.cshtml");
        }

        public async Task<IActionResult> Current([FromQuery] string? screenCode)
        {
            var result = await _permissionService.GetCurrentPermissions(screenCode);
            return Content(JsonConvert.SerializeObject(result), "application/json");
        }

        public async Task<IActionResult> Matrix()
        {
            var result = await _permissionService.GetMatrix();
            return Content(JsonConvert.SerializeObject(result), "application/json");
        }

        [HttpPost]
        public async Task<IActionResult> SetRolePermission([FromBody] RolePermissionSetDto dto)
        {
            Console.WriteLine("[Admin/PermissionController] SetRolePermission: " + JsonConvert.SerializeObject(dto));
            var result = await _permissionService.SetRolePermission(dto);
            if (result.code == "409")
            {
                return StatusCode(StatusCodes.Status409Conflict, result);
            }

            return Content(JsonConvert.SerializeObject(result), "application/json");
        }

        [HttpGet]
        public async Task<IActionResult> UserMatrix([FromQuery] long? userId)
        {
            var result = await _permissionService.GetUserMatrix(userId);
            return Content(JsonConvert.SerializeObject(result), "application/json");
        }

        [HttpPost]
        public async Task<IActionResult> SetUserPermission([FromBody] UserPermissionSetDto dto)
        {
            Console.WriteLine("[Admin/PermissionController] SetUserPermission: " + JsonConvert.SerializeObject(dto));
            var result = await _permissionService.SetUserPermission(dto);
            return Content(JsonConvert.SerializeObject(result), "application/json");
        }
    }
}
