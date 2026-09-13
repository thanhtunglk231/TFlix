using CoreLib.Dtos;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PermissionController : Controller
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
            return Ok(result);
        }

        public async Task<IActionResult> Matrix()
        {
            var result = await _permissionService.GetMatrix();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> SetRolePermission([FromBody] RolePermissionSetDto dto)
        {
            Console.WriteLine("[Admin/PermissionController] SetRolePermission: " + JsonConvert.SerializeObject(dto));
            var result = await _permissionService.SetRolePermission(dto);
            return Ok(result);
        }
    }
}
